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
// TODO: TBD: Not Honoring Multiple Workspaces - AS long as ShelvesetName include {0} it works fine.
namespace VsExt.AutoShelve {
    public class TfsAutoShelve : IDisposable {

        private const string Tfsext = "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt";

        private readonly DTE2 _dte;
        private readonly string _extensionName;
        private TeamFoundationServerExt _tfsExt;
        private Timer _timer;
        private ushort _maxShelvesets;
        private int _timerInterval;
        private string _shelvesetName;
        private string _workingDirectory;

        public bool IsRunning;

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

        public bool IsDateSpecificShelvesetName { get; private set; }

        public bool IsWorkspaceSpecificShelvesetName { get; private set; }

        public ushort MaximumShelvesets {
            get {
                return (IsDateSpecificShelvesetName) ? _maxShelvesets : (ushort)0;
            }
            set {
                _maxShelvesets = value;
            }
        }
        public string OutputPane { get; set; }

        public bool SuppressDialogs { get; set; }

        public int TimerInterval {
            get {
                return _timerInterval;
            }
            set {
                bool flag = IsRunning;
                if (flag) {
                    StopTimer();
                }
                _timerInterval = value;
                if (flag) {
                    StartTimer();
                }
            }
        }

        private int TimerPeriod {
            get {
                return 1000 * _timerInterval * 60;
            }
        }

        public static bool IsValidWorkspace(string absolutepath) {
            return (Workstation.Current.GetLocalWorkspaceInfo(absolutepath) != null);
        }

        public string WorkingDirectory {
            get {
                return _workingDirectory;
            }
            set {
                _workingDirectory = value;
                Workspace = string.IsNullOrWhiteSpace(value) ? null : Workstation.Current.GetLocalWorkspaceInfo(value);
                if (OnWorkspaceChanged != null) {
                        var workspaceChangedEventArg = new WorkspaceChangedEventArgs
                        {
                            IsWorkspaceValid = (Workspace != null)
                        };
                    OnWorkspaceChanged(this, workspaceChangedEventArg);
                }
            }
        }

        public WorkspaceInfo Workspace { get; set; }

        public TfsAutoShelve(string extensionName, DTE2 extDte) {
            _extensionName = extensionName;
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

        public void CreateShelveset() {
            var autoShelveCallback = GetAutoShelveCallback();
            autoShelveCallback(new object());
        }

        private TimerCallback GetAutoShelveCallback() {
            TimerCallback timerCallback = state => {
                if (Workspace != null) {
                    SaveShelveset();
                }
            }
            ;
            return timerCallback;
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

        private void InitializeTimer() {
            try {
                var autoResetEvent = new AutoResetEvent(false);
                _timer = new Timer(GetAutoShelveCallback(), autoResetEvent, -1, -1);
            } catch { }
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
                        if (IsWorkspaceSpecificShelvesetName)
                        {
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
                    var autoShelveEventArg = new ShelvesetCreatedEventArgs {ExecutionException = ex};
                    OnShelvesetCreated(this, autoShelveEventArg);
                }
            }
        }

        public void StartTimer() {
            _timer.Change(0, TimerPeriod);
            IsRunning = true;
            if (OnTimerStart != null) {
                OnTimerStart(this, new System.EventArgs());
            }
        }

        public void StopTimer() {
            if (!IsRunning) return;
            _timer.Change(-1, -1);
            IsRunning = false;
            if (OnTimerStop != null) {
                OnTimerStop(this, new System.EventArgs());
            }
        }

        public void Terminate() {
            StopTimer();
            WorkingDirectory = string.Empty;
        }

        public void ToggleTimerRunState() {
            if (IsRunning) {
                StopTimer();
            } else {
                StartTimer();
            }
        }

        public event EventHandler<ShelvesetCreatedEventArgs> OnShelvesetCreated;

        public event EventHandler OnTfsConnectionError;

        public event EventHandler OnTimerStart;

        public event EventHandler OnTimerStop;

        public event EventHandler<WorkspaceChangedEventArgs> OnWorkspaceChanged;

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
            if (disposeManaged) {
                // free managed resources
                if (_timer != null) {
                    StopTimer();
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

    }
}