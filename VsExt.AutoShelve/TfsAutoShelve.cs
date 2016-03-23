using System;
using System.Linq;
using System.Net;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TeamFoundation;
using VsExt.AutoShelve.EventArgs;
using System.Collections.Generic;

// Ref http://visualstudiogallery.msdn.microsoft.com/080540cb-e35f-4651-b71c-86c73e4a633d
namespace VsExt.AutoShelve
{
    public class TfsAutoShelve : SAutoShelveService, IAutoShelveService, IDisposable
    {
        private Timer _timer;
        private IServiceProvider serviceProvider;
        private string _extensionName = Resources.ExtensionName;

        public TfsAutoShelve(IServiceProvider sp)
        {
            serviceProvider = sp;
            try
            {
                var autoResetEvent = new AutoResetEvent(false);
                _timer = new Timer(GetAutoShelveCallback(), autoResetEvent, Timeout.Infinite, Timeout.Infinite);
            }
            catch { }
        }

        #region IAutoShelveService

        private bool _isRunning = false;
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
        }

        private ushort _maxShelvesets;
        public ushort MaximumShelvesets
        {
            get
            {
                return (IsDateSpecificShelvesetName) ? _maxShelvesets : (ushort)0;
            }
            set
            {
                _maxShelvesets = value;
            }
        }

        private string _shelvesetName;
        public string ShelvesetName
        {
            get
            {
                return _shelvesetName;
            }
            set
            {
                _shelvesetName = value;
                IsDateSpecificShelvesetName = string.Format(ShelvesetName, null, null, "IsDate", null, null).Contains("IsDate");
                IsWorkspaceSpecificShelvesetName = string.Format(ShelvesetName, "IsWorkspace", null, null, null, null).Contains("IsWorkspace");
            }
        }

        public double TimerInterval { get; set; }

        public void CreateShelveset(bool force = false)
        {
            try
            {
                if (TfsExt == null) return;
                var domainUri = WebUtility.UrlDecode(_tfsExt.ActiveProjectContext.DomainUri);
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
                    CreateShelveset(service, workspace, force);
                }
            }
            catch (Exception ex)
            {
                _tfsExt = null; // Force re-init on next attempt
                if (OnShelvesetCreated != null)
                {
                    var autoShelveEventArg = new ShelvesetCreatedEventArgs { ExecutionException = ex };
                    OnShelvesetCreated(this, autoShelveEventArg);
                }
            }
        }
        #region Events

        public event EventHandler<ShelvesetCreatedEventArgs> OnShelvesetCreated;

        public event EventHandler<TfsConnectionErrorEventArgs> OnTfsConnectionError;

        public event EventHandler OnStart;

        public event EventHandler OnStop;

        #endregion

        #endregion

        #region Private Members

        private bool IsDateSpecificShelvesetName { get; set; }

        private bool IsWorkspaceSpecificShelvesetName { get; set; }

        private TeamFoundationServerExt _tfsExt;
        private TeamFoundationServerExt TfsExt
        {
            get
            {
                if (_tfsExt != null) return _tfsExt;
                try
                {
                    DTE2 dte = (DTE2)this.serviceProvider.GetService(typeof(DTE));
                    var obj = (TeamFoundationServerExt)dte.GetObject("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt");
                    if (obj.ActiveProjectContext.DomainUri == null)
                        throw new NullReferenceException("Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt.ActiveProjectContext.DomainUri cannot be null");
                    else 
                        _tfsExt = obj;
                }
                catch (Exception ex)
                {
                    Stop();  // Disable timer to prevent Ref: Q&A "endless error dialogs" @ http://visualstudiogallery.msdn.microsoft.com/080540cb-e35f-4651-b71c-86c73e4a633d 
                    if (OnTfsConnectionError != null)
                    {
                        OnTfsConnectionError(this, new TfsConnectionErrorEventArgs{ConnectionError = ex });
                    }
                }
                return _tfsExt;
            }
        }

        private string CleanShelvesetName(string shelvesetName)
        {
            if (!string.IsNullOrWhiteSpace(shelvesetName))
            {
                string cleanName = shelvesetName.Replace("/", string.Empty).Replace(":", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("\\", string.Empty).Replace("|", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace(";", string.Empty);
                if (cleanName.Length > 64)
                {
                    return cleanName.Substring(0, 63);
                }
                return cleanName;
            }
            return shelvesetName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <param name="workspace"></param>
        /// <param name="force">True when the user manually initiates a ShelveSet via the Team menu or mapped shortcut key.</param>
        private void CreateShelveset(VersionControlServer service, Workspace workspace, bool force)
        {
            // Build event args for notification create shelveset result
            var autoShelveEventArg = new ShelvesetCreatedEventArgs();
            autoShelveEventArg.ShelvesetChangeCount = 0; // Shouldn't be necessary, but forcing it to be safe.

            try
            {
                // If there are no pending changes that have changed since the last shelveset then there is nothing to do
                bool isDelta = false;
                var pendingChanges = workspace.GetPendingChanges();
                int numPending = pendingChanges.Count();

                if (numPending > 0)
                {
                    if (!force)
                    {
                        var lastShelveset = GetPastShelvesets(service, workspace).FirstOrDefault();
                        if (lastShelveset == null)
                        {
                            // If there are pending changes and no shelveset yet exists, then create shelveset.
                            isDelta = true;
                        } else
                        {
                            // Compare numPending to shelvedChanges.Count();  Force shelveset if they differ
                            // Otherwise, resort to comparing file HashValues
                            var shelvedChanges = service.QueryShelvedChanges(lastShelveset).FirstOrDefault();
                            isDelta = (shelvedChanges == null || numPending != shelvedChanges.PendingChanges.Count()) || pendingChanges.DifferFrom(shelvedChanges.PendingChanges);
                        }
                    }
                    if (force || isDelta)
                    {
                        autoShelveEventArg.ShelvesetChangeCount = numPending;

                        // Build a new, valid shelve set name
                        var setname = string.Format(ShelvesetName, workspace.Name, workspace.OwnerName, DateTime.Now, workspace.OwnerName.GetDomain(), workspace.OwnerName.GetLogin());
                        setname = CleanShelvesetName(setname);

                        // Actually create a new Shelveset 
                        var shelveset = new Shelveset(service, setname, workspace.OwnerName);
                        autoShelveEventArg.ShelvesetName = setname;
                        shelveset.Comment = string.Format("Shelved by {0}. {1} items", _extensionName, numPending);
                        workspace.Shelve(shelveset, pendingChanges, ShelvingOptions.Replace);

                        // Clean up past Shelvesets
                        if (MaximumShelvesets > 0)
                        {
                            foreach (var set in GetPastShelvesets(service, workspace).Skip(MaximumShelvesets))
                            {
                                service.DeleteShelveset(set);
                                autoShelveEventArg.ShelvesetsPurgeCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _tfsExt = null; // Force re-init on next attempt
                autoShelveEventArg.ExecutionException = ex;
            }
            // Fire event for each VS instance to report results
            if (OnShelvesetCreated != null)
            {
                OnShelvesetCreated(this, autoShelveEventArg);
            }
        }

        private IEnumerable<Shelveset> GetPastShelvesets(VersionControlServer service, Workspace workspace)
        {
            var pastShelvesets = service.QueryShelvesets(null, workspace.OwnerName).Where(s => s.Comment != null && s.Comment.Contains(_extensionName));
            if (pastShelvesets != null && pastShelvesets.Count() > 0)
            {
                if (IsWorkspaceSpecificShelvesetName)
                {
                    pastShelvesets = pastShelvesets.Where(s => s.Name.Contains(workspace.Name));
                }
                return pastShelvesets.OrderByDescending(s => s.CreationDate);
            }
            else
            {
                return pastShelvesets;
            }

        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //// NOTE: Leave out the finalizer altogether if this class doesn't 
        //// own unmanaged resources itself, but leave the other methods
        //// exactly as they are. 
        //~TfsAutoShelve()
        //{
        //    // Finalizer calls Dispose(false)
        //    Dispose(false);
        //}

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposeManaged)
        {
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }

        #endregion

        private TimerCallback GetAutoShelveCallback()
        {
            TimerCallback timerCallback = state =>
            {
                CreateShelveset();
            }
            ;
            return timerCallback;
        }

        public void Start()
        {
            _timer.Change(TimeSpan.FromMinutes(TimerInterval), TimeSpan.FromMinutes(TimerInterval));
            _isRunning = true;
            if (OnStart != null)
            {
                OnStart(this, new System.EventArgs());
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _isRunning = false;
            if (OnStop != null)
            {
                OnStop(this, new System.EventArgs());
            }
        }

        #endregion

    }

}