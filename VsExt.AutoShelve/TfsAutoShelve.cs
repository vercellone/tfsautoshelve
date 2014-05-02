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
        private bool _isDate;
        private bool _isWorkspace;
        private ushort _maxShelvesets;
        private int _timerInterval;
        private string _shelvesetName;
        private bool _supressDialogs;
        private string _workingDirectory;

        public bool IsRunning;

        public string ShelvesetName {
            get {
                return _shelvesetName;
            }
            set {
                _shelvesetName = value;
                _isDate = string.Format(ShelvesetName, null, null, "IsDate").Contains("IsDate");
                _isWorkspace = string.Format(ShelvesetName, "IsWorkspace", null, null).Contains("IsWorkspace");
            }
        }

        public bool IsDateSpecificShelvesetName {
            get {
                return _isDate;
            }
        }
        public bool IsWorkspaceSpecificShelvesetName {
            get {
                return _isWorkspace;
            }
        }
        public ushort MaximumShelvesets {
            get {
                return (_isDate) ? _maxShelvesets : (ushort)0;
            }
            set {
                _maxShelvesets = value;
            }
        }
        public string OutputPane { get; set; }

        public bool SuppressDialogs {
            get {
                return _supressDialogs;
            }
            set {
                _supressDialogs = value;
            }
        }

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
                        WorkspaceChangedEventArgs workspaceChangedEventArg = new WorkspaceChangedEventArgs();
                        workspaceChangedEventArg.IsWorkspaceValid = (Workspace != null);
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
                                ShelvesetCreatedEventArgs autoShelveEventArg = new ShelvesetCreatedEventArgs();
                                string setname = string.Format(ShelvesetName, workspaceInfo.Name, workspaceInfo.OwnerName, DateTime.Now);
                                setname = CleanShelvesetName(setname);

                                Shelveset shelveset = new Shelveset(service, setname, workspaceInfo.OwnerName);
                                autoShelveEventArg.ShelvesetChangeCount += numPending;
                                autoShelveEventArg.ShelvesetName = setname;

                                shelveset.Comment = string.Format("Shelved by {0}. Items in shelve set: {1}", _extensionName, numPending);
                                workspace.Shelve(shelveset, pendingChanges, ShelvingOptions.Replace);
                                if (MaximumShelvesets > 0) {
                                    var autoShelvesets = service.QueryShelvesets(null, workspaceInfo.OwnerName).Where(s => s.Comment != null && s.Comment.Contains(_extensionName));
                                    if (IsWorkspaceSpecificShelvesetName) {
                                        autoShelvesets = autoShelvesets.Where(s => s.Name.Contains(workspaceInfo.Name));
                                    }
                                    foreach (Shelveset set in autoShelvesets.OrderByDescending(s => s.CreationDate).Skip(MaximumShelvesets)) {
                                        service.DeleteShelveset(set);
                                        autoShelveEventArg.ShelvesetsPurgeCount++;
                                    }
                                }
                                if (OnShelvesetCreated != null) {
                                    OnShelvesetCreated(this, autoShelveEventArg);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                if (OnShelvesetCreated != null) {
                    ShelvesetCreatedEventArgs autoShelveEventArg = new ShelvesetCreatedEventArgs();
                    autoShelveEventArg.ExecutionException = ex;
                    OnShelvesetCreated(this, autoShelveEventArg);
                }
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