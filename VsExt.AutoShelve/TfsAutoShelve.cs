using System;
using System.Linq;
using System.Net;
using System.Threading;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TeamFoundation;
using VsExt.AutoShelve.EventArgs;

// Ref http://visualstudiogallery.msdn.microsoft.com/080540cb-e35f-4651-b71c-86c73e4a633d
namespace VsExt.AutoShelve {
    public class TfsAutoShelve : IAutoShelveService, IDisposable {

        private IServiceProvider serviceProvider;
        private const string Tfsext = "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt";

        private readonly DTE2 _dte;
        private string _extensionName = Resources.ExtensionName;
        private TeamFoundationServerExt _tfsExt;
        private ushort _maxShelvesets;
        private string _shelvesetName;

        public string ShelvesetName {
            get {
                return _shelvesetName;
            }
            set {
                _shelvesetName = value;
                IsDateSpecificShelvesetName = string.Format(ShelvesetName, null, null, "IsDate").Contains("IsDate");
                IsWorkspaceSpecificShelvesetName = string.Format(ShelvesetName, "IsWorkspace", null, null).Contains("IsWorkspace");
            }
        }

        private bool IsDateSpecificShelvesetName { get; set; }

        private bool IsWorkspaceSpecificShelvesetName { get; set; }

        public ushort MaximumShelvesets {
            get {
                return (IsDateSpecificShelvesetName) ? _maxShelvesets : (ushort)0;
            }
            set {
                _maxShelvesets = value;
            }
        }

        //public static bool IsValidWorkspace(string absolutepath)
        //{
        //    //Microsoft.TeamFoundation.VersionControl.Client.PendingChangeEventHandler
        //    //    Microsoft.TeamFoundation.Client.TeamFoundationWorkspaceContextMonitor

        //    return Workstation.Current.IsMapped(absolutepath) || (Workstation.Current.GetLocalWorkspaceInfo(absolutepath) != null);
        //}

        public TfsAutoShelve(IServiceProvider sp, DTE2 extDte) {
            //serviceProvider = sp; 

            _dte = extDte;
            InitializeTimer();
        }

        private string CleanShelvesetName(string shelvesetName) {
            if (!string.IsNullOrWhiteSpace(shelvesetName)) {
                string cleanName = shelvesetName.Replace("/", string.Empty).Replace(":", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("\\", string.Empty).Replace("|", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace(";", string.Empty);
                if (cleanName.Length > 64) {
                    return cleanName.Substring(0, 63);
                }
                return cleanName;
            }
            return shelvesetName;
        }

        private TeamFoundationServerExt TfsExt {
            get {
                if (_tfsExt != null) return _tfsExt;
                var obj = (TeamFoundationServerExt)_dte.GetObject(Tfsext);
                if (obj.ActiveProjectContext.DomainUri != null && obj.ActiveProjectContext.ProjectUri != null) {
                    _tfsExt = obj;
                } else {
                    StopTimer();  // Disable timer to prevent Ref: Q&A "endless error dialogs" @ http://visualstudiogallery.msdn.microsoft.com/080540cb-e35f-4651-b71c-86c73e4a633d 
                    if (OnTfsConnectionError != null) {
                        OnTfsConnectionError(this, new System.EventArgs());
                    }
                }
                return _tfsExt;
            }
        }

        public void SaveShelveset() {
            try {
                if (TfsExt == null) return;
                var domainUri = _tfsExt.ActiveProjectContext.DomainUri;
                var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(domainUri));
                teamProjectCollection.Credentials = CredentialCache.DefaultNetworkCredentials;
                teamProjectCollection.EnsureAuthenticated();

                var service = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));
                var allLocalWorkspaceInfo = Workstation.Current.GetAllLocalWorkspaceInfo();

                foreach (var workspaceInfo in allLocalWorkspaceInfo)
                {
                    // Replace(/,"") before comparing domainUri to prevent: "TFS Auto Shelve shelved 0 pending changes. Shelveset Name: "
                    if (workspaceInfo.MappedPaths.Length <= 0 ||
                        workspaceInfo.ServerUri.ToString().Replace("/", string.Empty) !=
                        domainUri.Replace("/", string.Empty)) continue;
                    var workspace = service.GetWorkspace(workspaceInfo);
                    SaveShelveSet(workspace);
                }
            } catch (Exception ex) {
                if (OnShelvesetCreated != null) {
                    var autoShelveEventArg = new ShelvesetCreatedEventArgs {ExecutionException = ex};
                    OnShelvesetCreated(this, autoShelveEventArg);
                }
            }
        }


        public void SaveShelveset(Workspace workspace) {
            try {
                if (TfsExt == null) return;
                var domainUri = _tfsExt.ActiveProjectContext.DomainUri;
                var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(domainUri));
                teamProjectCollection.Credentials = CredentialCache.DefaultNetworkCredentials;
                teamProjectCollection.EnsureAuthenticated();

                var service = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));
                var allLocalWorkspaceInfo = Workstation.Current.GetAllLocalWorkspaceInfo();

                foreach (var workspaceInfo in allLocalWorkspaceInfo) {
                    // Replace(/,"") before comparing domainUri to prevent: "TFS Auto Shelve shelved 0 pending changes. Shelveset Name: "
                    if (workspaceInfo.MappedPaths.Length <= 0 ||
                        workspaceInfo.ServerUri.ToString().Replace("/", string.Empty) !=
                        domainUri.Replace("/", string.Empty)) continue;
                    var workspace = service.GetWorkspace(workspaceInfo);
                    var pendingChanges = workspace.GetPendingChanges();

                    var numPending = pendingChanges.Count();
                    if (numPending <= 0) continue;
                    var autoShelveEventArg = new ShelvesetCreatedEventArgs();
                    var setname = string.Format(ShelvesetName, workspaceInfo.Name, workspaceInfo.OwnerName, DateTime.Now);
                    setname = CleanShelvesetName(setname);

                    var shelveset = new Shelveset(service, setname, workspaceInfo.OwnerName);
                    autoShelveEventArg.ShelvesetChangeCount += numPending;
                    autoShelveEventArg.ShelvesetName = setname;

                    shelveset.Comment = string.Format("Shelved by {0}. Items in shelve set: {1}", _extensionName, numPending);
                    workspace.Shelve(shelveset, pendingChanges, ShelvingOptions.Replace);
                    if (MaximumShelvesets > 0) {
                        var autoShelvesets = service.QueryShelvesets(null, workspaceInfo.OwnerName).Where(s => s.Comment != null && s.Comment.Contains(_extensionName));
                        if (IsWorkspaceSpecificShelvesetName) {
                            var info = workspaceInfo;
                            autoShelvesets = autoShelvesets.Where(s => s.Name.Contains(info.Name));
                        }
                        foreach (var set in autoShelvesets.OrderByDescending(s => s.CreationDate).Skip(MaximumShelvesets)) {
                            service.DeleteShelveset(set);
                            autoShelveEventArg.ShelvesetsPurgeCount++;
                        }
                    }
                    if (OnShelvesetCreated != null) {
                        OnShelvesetCreated(this, autoShelveEventArg);
                    }
                }
            } catch (Exception ex) {
                if (OnShelvesetCreated != null) {
                    var autoShelveEventArg = new ShelvesetCreatedEventArgs { ExecutionException = ex };
                    OnShelvesetCreated(this, autoShelveEventArg);
                }
            }
        }

        public event EventHandler<ShelvesetCreatedEventArgs> OnShelvesetCreated;

        public event EventHandler OnTfsConnectionError;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // NOTE: Leave out the finalizer altogether if this class doesn't 
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are. 
        ~TfsAutoShelve() {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposeManaged) {
        }
    }
}