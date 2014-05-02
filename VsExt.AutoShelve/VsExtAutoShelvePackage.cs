﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsExt.AutoShelve.EventArgs;
using VsExt.AutoShelve.IO;
using VsExt.AutoShelve.Packaging;
using ActivityLog = VsExt.AutoShelve.IO.ActivityLog;

namespace VsExt.AutoShelve {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [ProvideAutoLoad("{F1536EF8-92EC-443C-9ED7-FDADF150DA82}")] // VSConstants.UICONTEXT.SolutionExists_guid
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute is used to include custom options in the Tools->Options dialog
    [ProvideOptionPage(typeof(OptionsPageGeneral), "TFS Auto Shelve", "General", 101, 106, true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "3.6", IconResourceID = 400)]
    [Guid(GuidList.GuidAutoShelvePkgString)]
    public class VsExtAutoShelvePackage : Package, IVsSolutionEvents, IDisposable {

        private TfsAutoShelve _autoShelve;
        private DTE2 _dte;
        private string _extName;
        private OleMenuCommand _menuAutoShelveNow;
        private OleMenuCommand _menuRunState;
        private string _menuTextRunning;
        private string _menuTextStopped;
        private OptionsPageGeneral _options;
        private uint _solutionEventsCookie;
        private IVsSolution2 _solutionService;
        private Timer _timer;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VsExtAutoShelvePackage() {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this));
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        private void autoShelve_OnShelvesetCreated(object sender, ShelvesetCreatedEventArgs e) {
            if (e.ExecutionSuccess) {
              var str = string.Format("Shelved {0} pending change{1} to Shelveset Name: {2}", e.ShelvesetChangeCount, 
                e.ShelvesetChangeCount != 1 ? "s" :"", e.ShelvesetName);
                if (e.ShelvesetsPurgeCount > 0) {
                    str += string.Format(" | Maximum Shelvesets: {0} | Deleted: {1}", _autoShelve.MaximumShelvesets, e.ShelvesetsPurgeCount);
                }
                WriteToStatusBar(str);
                WriteToOutputWindow(str);
            } else {
                WriteToStatusBar(string.Format("{0} encountered an error.", _extName));
                WriteToOutputWindow(e.ExecutionException.Message);
                WriteToOutputWindow(e.ExecutionException.StackTrace);
                ActivityLog.WriteToActivityLog(e.ExecutionException.Message, e.ExecutionException.StackTrace);
            }
        }

        private void autoShelve_OnTfsConnectionError(object sender,System.EventArgs e) {
            WriteToOutputWindow(Resources.ErrorNotConnected);
            if (!_autoShelve.SuppressDialogs) {
                MessageBox.Show(Resources.ErrorNotConnected, _extName, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void autoShelve_OnTimerStart(object sender,System.EventArgs e) {
            DisplayRunState();
        }

        private void autoShelve_OnTimerStop(object sender,System.EventArgs e) {
            DisplayRunState();
        }

        private void autoShelve_OnWorkspaceChanged(object sender, WorkspaceChangedEventArgs e) {
            _menuAutoShelveNow.Enabled = e.IsWorkspaceValid;
            _menuRunState.Enabled = e.IsWorkspaceValid;
        }

        private void DetachAutoShelveEvents() {
            if (_autoShelve != null) {
                _autoShelve.CreateShelveset();
                _autoShelve.Terminate();
                _autoShelve.OnShelvesetCreated -= autoShelve_OnShelvesetCreated;
                _autoShelve.OnTfsConnectionError -= autoShelve_OnTfsConnectionError;
                _autoShelve.OnTimerStart -= autoShelve_OnTimerStart;
                _autoShelve.OnTimerStop -= autoShelve_OnTimerStop;
                _autoShelve.OnWorkspaceChanged -= autoShelve_OnWorkspaceChanged;
                _options.OnOptionsChanged -= Options_OnOptionsChanged;
            }
            if (_solutionService != null) {
                _solutionService.UnadviseSolutionEvents(_solutionEventsCookie);
            }
        }

        private void DisplayRunState() {
            var str1 = string.Format("{0} is{1} running", _extName, _autoShelve.IsRunning ? string.Empty : " not");
            WriteToStatusBar(str1);
            WriteToOutputWindow(str1);
            ToggleMenuCommandRunStateText(_menuRunState);
        }

        private void InitializeAutoShelve(string workingDirectory) {
            InitializeSolutionServiceEvents();
            try {
                _autoShelve = new TfsAutoShelve(_extName, _dte);

                // Tools->Options event wire-up
                _options.OnOptionsChanged += Options_OnOptionsChanged;

                // Event Wire-up
                _autoShelve.OnShelvesetCreated += autoShelve_OnShelvesetCreated;
                _autoShelve.OnTfsConnectionError += autoShelve_OnTfsConnectionError;
                _autoShelve.OnTimerStart += autoShelve_OnTimerStart;
                _autoShelve.OnTimerStop += autoShelve_OnTimerStop;
                _autoShelve.OnWorkspaceChanged += autoShelve_OnWorkspaceChanged;

                // Property Initialization
                _autoShelve.MaximumShelvesets = _options.MaximumShelvesets;
                _autoShelve.OutputPane = _options.OutputPane;
                _autoShelve.ShelvesetName = _options.ShelvesetName;
                _autoShelve.TimerInterval = _options.TimerSaveInterval;
                _autoShelve.SuppressDialogs = _options.SuppressDialogs;
                _autoShelve.WorkingDirectory = workingDirectory;

                _autoShelve.StartTimer();
            } catch {
                if (_autoShelve != null) {
                    _options.OnOptionsChanged -= Options_OnOptionsChanged;

                    _autoShelve.OnShelvesetCreated -= autoShelve_OnShelvesetCreated;
                    _autoShelve.OnTfsConnectionError -= autoShelve_OnTfsConnectionError;
                    _autoShelve.OnTimerStart -= autoShelve_OnTimerStart;
                    _autoShelve.OnTimerStop -= autoShelve_OnTimerStop;
                    _autoShelve.OnWorkspaceChanged -= autoShelve_OnWorkspaceChanged;
                }
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));
            base.Initialize();

            // Internal
            _extName = Resources.ExtensionName;
            _menuTextRunning = string.Concat(_extName, " (Running)");
            _menuTextStopped = string.Concat(_extName, " (Not Running)");

            // InitializePackageServices
            ActivityLog.Log = GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
            _dte = (DTE2)GetGlobalService(typeof(DTE));
            _solutionService = (IVsSolution2)GetGlobalService(typeof(SVsSolution));

            //InitializeOutputWindowPane
            if (_dte != null) {
                _dte.ToolWindows.OutputWindow.OutputWindowPanes.Add("TFS Auto Shelve");
            }

            InitializeSolutionServiceEvents();
            InitializeMenuCommands();
            InitializeTimer();
        }

        private void InitializeSolutionServiceEvents() {
            if (_solutionService != null) {
                _solutionService.AdviseSolutionEvents(this, out _solutionEventsCookie);
                _options = (OptionsPageGeneral)GetDialogPage(typeof(OptionsPageGeneral));
            }
        }

        private void InitializeMenuCommands() {
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null) {
                var commandId = new CommandID(GuidList.GuidAutoShelveCmdSet, PkgCmdIdList.CmdidAutoShelve);
                var oleMenuCommand = new OleMenuCommand(MenuItemCallbackAutoShelveRunState, commandId)
                {
                    Text = _menuTextStopped,
                    Enabled = false
                };
                _menuRunState = oleMenuCommand;
                mcs.AddCommand(_menuRunState);

                var commandId1 = new CommandID(GuidList.GuidAutoShelveCmdSet, PkgCmdIdList.CmdidAutoShelveNow);
                var oleMenuCommand1 = new OleMenuCommand(MenuItemCallbackRunNow, commandId1) {Enabled = false};
                _menuAutoShelveNow = oleMenuCommand1;
                mcs.AddCommand(_menuAutoShelveNow);
            }
        }

        private void InitializeTimer() {
            try {
                _timer = new Timer {Enabled = false, Interval = 9000};
                EventHandler eventHandler = (sender, e) => {
                    _dte.StatusBar.Text = string.Empty;
                    _timer.Enabled = false;
                };
                _timer.Tick += eventHandler;
// ReSharper disable once EmptyGeneralCatchClause
            } catch
            { }
        }

        #endregion

        #region IVsSolutionEvents

        private void MenuItemCallbackAutoShelveRunState(object sender,System.EventArgs e) {
            _autoShelve.ToggleTimerRunState();
        }

        private void MenuItemCallbackRunNow(object sender,System.EventArgs e) {
            _autoShelve.SaveShelveset();
        }

        public int OnAfterCloseSolution(object pUnkReserved) {
            DetachAutoShelveEvents();
            return 0;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) { return 0; }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            if (_autoShelve == null || _autoShelve.Workspace == null) {
                object projectObj;
                pHierarchy.GetProperty(Microsoft.VisualStudio.VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectObj);
                var project = (Project)projectObj;
                if (project != null && !string.IsNullOrWhiteSpace(project.FullName)) {
                    var projDirectory = Path.GetDirectoryName(project.FullName);
                    if (TfsAutoShelve.IsValidWorkspace(projDirectory)) {
                        InitializeAutoShelve(projDirectory);
                    }
                }
            }
            return 0;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
            if (!string.IsNullOrWhiteSpace(_dte.Solution.FullName)) {
                var slnDirectory = Path.GetDirectoryName(_dte.Solution.FullName);
                if (TfsAutoShelve.IsValidWorkspace(slnDirectory)) {
                    InitializeAutoShelve(slnDirectory);
                }
            }
            return 0;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) { return 0; }

        public int OnBeforeCloseSolution(object pUnkReserved) { return 0; }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) { return 0; }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) { return 0; }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) { return 0; }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) { return 0; }

        #endregion

        #region Local Methods

        private void Options_OnOptionsChanged(object sender, OptionsChangedEventArgs e) {
            if (_autoShelve != null) {
                _autoShelve.MaximumShelvesets = e.MaximumShelvesets;
                _autoShelve.OutputPane = e.OutputPane;
                _autoShelve.ShelvesetName = e.ShelvesetName;
                _autoShelve.SuppressDialogs = e.SuppressDialogs;
                _autoShelve.TimerInterval = e.Interval;
            }
        }

        private void ToggleMenuCommandRunStateText(object sender) {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null) {
                if (menuCommand.CommandID.Guid == GuidList.GuidAutoShelveCmdSet) {
                    menuCommand.Text = _autoShelve.IsRunning ? _menuTextRunning : _menuTextStopped;
                }
            }
        }

        private void WriteToOutputWindow(string outputText) {
            if (!string.IsNullOrWhiteSpace(_autoShelve.OutputPane))
                _dte.ToolWindows.OutputWindow.OutputWindowPanes.Item(_autoShelve.OutputPane).OutputString(string.Concat(outputText, "\n"));
        }

        private void WriteToStatusBar(string text) {
            _dte.StatusBar.Text = text;
            _timer.Enabled = true;
        }

        #endregion

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~VsExtAutoShelvePackage() {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected override void Dispose(bool disposeManaged) {
            if (disposeManaged) {
                // free managed resources
                if (_timer != null) {
                    _timer.Enabled = false;
                    _timer.Dispose();
                    _timer = null;
                }
                if (_autoShelve != null) {
                    _autoShelve.Dispose();
                    _autoShelve = null;
                }
            }
            base.Dispose(disposeManaged);
        }

    }
}
