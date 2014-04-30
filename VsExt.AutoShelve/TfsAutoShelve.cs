using System;
using System.Linq;
using System.Net;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TeamFoundation;

// Ref http://visualstudiogallery.msdn.microsoft.com/080540cb-e35f-4651-b71c-86c73e4a633d
// TODO: TBD: Not Honoring Multiple Workspaces - AS long as ShelvesetName include {0} it works fine.
namespace VsExt.AutoShelve {
    public class TfsAutoShelve : IDisposable {

        private const string _TFSEXT = "Microsoft.VisualStudio.TeamFoundation.TeamFoundationServerExt";

        private DTE2 _dte;
        private string _extensionName;
        private TeamFoundationServerExt _tfsExt;
        private Timer _timer;
        private int _timerInterval;
        private string _workingDirectory;

        public bool IsRunning;
        public string ShelveSetName;

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

        public string WorkingDirectory {
            get {
                return _workingDirectory;
            }
            set {
                _workingDirectory = value;
                if (!string.IsNullOrWhiteSpace(value)) {
                    Workspace = Workstation.Current.GetLocalWorkspaceInfo(value);
                    if (OnWorkSpaceDiscovery != null) {
                        WorkSpaceDiscoveryEventArgs workSpaceDiscoveryEventArg = new WorkSpaceDiscoveryEventArgs();
                        workSpaceDiscoveryEventArg.IsWorkspaceDiscovered = (Workspace != null);
                        OnWorkSpaceDiscovery(this, workSpaceDiscoveryEventArg);
                    }
                }
            }
        }

        public WorkspaceInfo Workspace { get; set; }

        public TfsAutoShelve(string extensionName, DTE2 extDte) {
            _extensionName = extensionName;
            _dte = extDte;
            InitializeTimer();
        }

        private string CleanShelveSetName(string shelvesetName) {
            if (!string.IsNullOrWhiteSpace(shelvesetName)) {
                string cleanName = shelvesetName.Replace("/", string.Empty).Replace(":", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("\\", string.Empty).Replace("|", string.Empty).Replace("*", string.Empty).Replace("?", string.Empty).Replace(";", string.Empty);
                if (cleanName.Length > 64) {
                    return cleanName.Substring(0, 63);
                }
                return cleanName;
            }
            return shelvesetName;
        }

        public void CreateShelveSet() {
            TimerCallback autoShelveCallback = GetAutoShelveCallback();
            autoShelveCallback(new object());
        }

        public TimerCallback GetAutoShelveCallback() {
            TimerCallback timerCallback = (object state) => {
                if (Workspace != null) {
                    SaveShelveset();
                }
            }
            ;
            return timerCallback;
        }

        private TeamFoundationServerExt TfsExt {
            get {
                if (_tfsExt == null) {
                    TeamFoundationServerExt obj = (TeamFoundationServerExt)_dte.GetObject(_TFSEXT);
                    if (obj.ActiveProjectContext.DomainUri != null && obj.ActiveProjectContext.ProjectUri != null) {
                        _tfsExt = obj;
                    } else {
                        StopTimer();  // Disable timer to prevent Ref: Q&A "endless error dialogs" @ http://visualstudiogallery.msdn.microsoft.com/080540cb-e35f-4651-b71c-86c73e4a633d 
                        if (OnTfsConnectionError != null) {
                            OnTfsConnectionError(this, new EventArgs());
                        }
                    }
                }
                return _tfsExt;
            }
        }

        private void InitializeTimer() {
            try {
                AutoResetEvent autoResetEvent = new AutoResetEvent(false);
                _timer = new Timer(GetAutoShelveCallback(), autoResetEvent, -1, -1);
            } catch { }
        }

        public void SaveShelveset() {
            AutoShelveEventArgs autoShelveEventArg = new AutoShelveEventArgs();
            try {
                if (TfsExt != null) {
                    string domainUri = _tfsExt.ActiveProjectContext.DomainUri;
                    TfsTeamProjectCollection teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(domainUri));
                    teamProjectCollection.Credentials = CredentialCache.DefaultNetworkCredentials;
                    teamProjectCollection.EnsureAuthenticated();

                    VersionControlServer service = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));
                    WorkspaceInfo[] allLocalWorkspaceInfo = Workstation.Current.GetAllLocalWorkspaceInfo();

                    for (int n = 0; n < allLocalWorkspaceInfo.Length; n++) {
                        WorkspaceInfo workspaceInfo = allLocalWorkspaceInfo[n];
                        // Replace(/,"") before comparing domainUri to prevent: "TFS Auto Shelve shelved 0 pending changes. Shelveset Name: "
                        if (workspaceInfo.MappedPaths.Length > 0 && workspaceInfo.ServerUri.ToString().Replace("/", string.Empty) == domainUri.Replace("/", string.Empty)) {
                            Workspace workspace = service.GetWorkspace(workspaceInfo);
                            PendingChange[] pendingChanges = workspace.GetPendingChanges();
                            int numPending = pendingChanges.Count<PendingChange>();
                            if (numPending > 0) {
                                string setname = string.Format(ShelveSetName, workspaceInfo.Name, workspaceInfo.OwnerName, DateTime.Now);
                                setname = CleanShelveSetName(setname);

                                Shelveset shelveset = new Shelveset(service, setname, workspaceInfo.OwnerName);
                                autoShelveEventArg.ShelvesetCount += numPending;
                                autoShelveEventArg.ShelvesetName = setname;

                                shelveset.Comment = string.Format("Shelved by {0}. Items in shelve set: {1}", _extensionName, numPending);
                                workspace.Shelve(shelveset, pendingChanges, ShelvingOptions.Replace);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                autoShelveEventArg.ExecutionException = ex;
            }
            if (OnExecution != null) {
                OnExecution(this, autoShelveEventArg);
            }
        }

        public void StartTimer() {
            _timer.Change(0, TimerPeriod);
            IsRunning = true;
            if (OnTimerStart != null) {
                OnTimerStart(this, new EventArgs());
            }
        }

        public void StopTimer() {
            if (IsRunning) {
                _timer.Change(-1, -1);
                IsRunning = false;
                if (OnTimerStop != null) {
                    OnTimerStop(this, new EventArgs());
                }
            }
        }

        public void Terminate() {
            StopTimer();
            if (OnWorkSpaceDiscovery != null) {
                WorkSpaceDiscoveryEventArgs workSpaceDiscoveryEventArg = new WorkSpaceDiscoveryEventArgs();
                workSpaceDiscoveryEventArg.IsWorkspaceDiscovered = false;
                OnWorkSpaceDiscovery(this, workSpaceDiscoveryEventArg);
            }
        }

        public void ToggleTimerRunState() {
            if (IsRunning) {
                StopTimer();
            } else {
                StartTimer();
            }
        }

        public event EventHandler<AutoShelveEventArgs> OnExecution;

        public event EventHandler OnTfsConnectionError;

        public event EventHandler OnTimerStart;

        public event EventHandler OnTimerStop;

        public event EventHandler<WorkSpaceDiscoveryEventArgs> OnWorkSpaceDiscovery;

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