using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VsExt.AutoShelve.EventArgs;
using VsExt.AutoShelve.IO;
using VsExt.AutoShelve.Packaging;

namespace VsExt.AutoShelve
{
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
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)] // https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.uicontextguids80.aspx
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute is used to include custom options in the Tools->Options dialog
    [ProvideOptionPage(typeof(OptionsPageGeneral), "TFS Auto Shelve", "General", 101, 106, true)]
    [ProvideService(typeof(TfsAutoShelve))]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "5.4", IconResourceID = 400)]
    [Guid(GuidList.GuidAutoShelvePkgString)]
    public class VsExtAutoShelvePackage : Package, IVsSolutionEvents, IDisposable
    {

        private IAutoShelveService _autoShelve;
        private DTE2 _dte;
        private IVsActivityLog _log;
        private OleMenuCommand _menuAutoShelveNow;
        private OleMenuCommand _menuRunState;
        private string _extName = Resources.ExtensionName;
        private string _menuTextRunning = string.Concat(Resources.ExtensionName, " (Running)");
        private string _menuTextStopped = string.Concat(Resources.ExtensionName, " (Not Running)");
        private OptionsPageGeneral _options;
        private uint _solutionEventsCookie;
        private IVsSolution2 _solutionService;
        private DebuggerEvents _debuggerEvents;
        private bool _isPaused;
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VsExtAutoShelvePackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this));

            IServiceContainer serviceContainer = this as IServiceContainer;
            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            serviceContainer.AddService(typeof(SAutoShelveService), callback, true);
            //serviceContainer.AddService(typeof(SMyLocalService), callback); 
        }

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SAutoShelveService) == serviceType)
            {
                if (_autoShelve == null)
                    _autoShelve = new TfsAutoShelve(this);
                return _autoShelve;
            }
            //if (typeof(SMyLocalService) == serviceType)
            //    return new MyLocalService(this);
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        private void autoShelve_OnShelvesetCreated(object sender, ShelvesetCreatedEventArgs e)
        {
            if (e.ExecutionSuccess)
            {
                if (e.ShelvesetChangeCount == 0)
                {
                    //WriteToOutputWindow(".");
                }
                else
                {
                    var str = string.Format("Shelved {0} pending change{1} to Shelveset Name: {2}", e.ShelvesetChangeCount,
                      e.ShelvesetChangeCount != 1 ? "s" : "", e.ShelvesetName);
                    if (e.ShelvesetsPurgeCount > 0)
                    {
                        str += string.Format(" | Maximum Shelvesets: {0} | Deleted: {1}", _autoShelve.MaximumShelvesets, e.ShelvesetsPurgeCount);
                    }
                    WriteToStatusBar(str);
                    WriteLineToOutputWindow(str);
                }
            }
            else
            {
                WriteException(e.ExecutionException);
            }
        }

        private void autoShelve_OnTfsConnectionError(object sender, TfsConnectionErrorEventArgs e)
        {
            WriteLineToOutputWindow(Resources.ErrorNotConnected);
            WriteException(e.ConnectionError);
        }

        private void autoShelve_OnStart(object sender, System.EventArgs e)
        {
            DisplayRunState();
        }

        private void autoShelve_OnStop(object sender, System.EventArgs e)
        {
            DisplayRunState();
        }

        private void DisplayRunState()
        {
            string str1;
            if (_isPaused)
            {
                str1 = string.Format("{0} paused while Debugging", _extName);
            }
            else
            {
                str1 = string.Format("{0} is{1} running", _extName, _autoShelve.IsRunning ? string.Empty : " not");
            }
            WriteToStatusBar(str1);
            WriteLineToOutputWindow(str1);
            ToggleMenuCommandRunStateText(_menuRunState);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));

                base.Initialize();

                // InitializePackageServices
                _dte = (DTE2)GetGlobalService(typeof(DTE));

                _log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;

                // Initialize Tools->Options Page
                _options = (OptionsPageGeneral)GetDialogPage(typeof(OptionsPageGeneral));

                // Initialize Solution Service Events
                _solutionService = (IVsSolution2)GetGlobalService(typeof(SVsSolution));
                if (_solutionService != null)
                {
                    _solutionService.AdviseSolutionEvents(this, out _solutionEventsCookie);
                }
				
                _debuggerEvents = (EnvDTE.DebuggerEvents)_dte.Events.DebuggerEvents;
                _debuggerEvents.OnEnterRunMode += new _dispDebuggerEvents_OnEnterRunModeEventHandler(OnEnterRunMode); ;
                _debuggerEvents.OnEnterDesignMode += new _dispDebuggerEvents_OnEnterDesignModeEventHandler(OnEnterDesignMode); ;

                //InitializeOutputWindowPane
                if (_dte != null
                    && _dte.ToolWindows.OutputWindow.OutputWindowPanes.Cast<OutputWindowPane>().All(p => p.Name != _options.OutputPane))
                {
                    _dte.ToolWindows.OutputWindow.OutputWindowPanes.Add(_options.OutputPane);
                }

                InitializeMenus();
                InitializeAutoShelve();
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Register your own package service)
        /// http://technet.microsoft.com/en-us/office/bb164693(v=vs.71).aspx
        /// http://blogs.msdn.com/b/aaronmar/archive/2004/03/12/88646.aspx
        /// http://social.msdn.microsoft.com/Forums/vstudio/en-US/be755076-6e07-4025-93e7-514cd4019dcb/register-own-service?forum=vsx
        /// IVsRunningDocumentTable rdt = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        /// rdt.AdviseRunningDocTableEvents(new YourRunningDocTableEvents());
        /// rdt.GetDocumentInfo(docCookie, ...)
        /// One of the out params is RDT_ProjSlnDocument; this will be set for your solution file. Note this flag also covers projects. Once you have sufficiently determined it is your solution you're set.
        /// 
        /// http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivsrunningdoctableevents.onaftersave.aspx
        /// http://social.msdn.microsoft.com/Forums/vstudio/it-IT/fd513e71-bb23-4de0-b631-35bfbdfdd4f5/visual-studio-isolated-shell-onsolutionsaved-event?forum=vsx
        /// </remarks>
        private void InitializeAutoShelve()
        {
            _autoShelve = GetGlobalService(typeof(SAutoShelveService)) as TfsAutoShelve;
            if (_autoShelve != null)
            {
                // Property Initialization
                _autoShelve.MaximumShelvesets = _options.MaximumShelvesets;
                _autoShelve.ShelvesetName = _options.ShelvesetName;
                _autoShelve.TimerInterval = _options.TimerSaveInterval;
            }
            AttachEvents();
        }

        private void InitializeMenus()
        {
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
            {
                var commandId = new CommandID(GuidList.GuidAutoShelveCmdSet, PkgCmdIdList.CmdidAutoShelve);
                var oleMenuCommand = new OleMenuCommand(MenuItemCallbackAutoShelveRunState, commandId);
                oleMenuCommand.Text = _menuTextStopped;
                _menuRunState = oleMenuCommand;
                mcs.AddCommand(_menuRunState);

                var commandId1 = new CommandID(GuidList.GuidAutoShelveCmdSet, PkgCmdIdList.CmdidAutoShelveNow);
                var oleMenuCommand1 = new OleMenuCommand(MenuItemCallbackRunNow, commandId1);
                _menuAutoShelveNow = oleMenuCommand1;
                mcs.AddCommand(_menuAutoShelveNow);
            }
        }

        #endregion

        #region Package Menu Commands

        private void MenuItemCallbackAutoShelveRunState(object sender, System.EventArgs e)
        {
            try
            {
                _isPaused = false; // this prevents un-pause following a manual start/stop
                if (_autoShelve.IsRunning)
                {
                    _autoShelve.Stop();
                }
                else
                {
                    _autoShelve.Start();
                }
            }
            catch
            {
                // swallow exceptions
            }
        }

        private void MenuItemCallbackRunNow(object sender, System.EventArgs e)
        {
            _autoShelve.CreateShelveset(true);
        }

        #endregion

        #region Local Methods

        private void AttachEvents()
        {
            if (_autoShelve != null)
            {
                _autoShelve.OnStop += autoShelve_OnStop;
                _autoShelve.OnStart += autoShelve_OnStart;
                _autoShelve.OnShelvesetCreated += autoShelve_OnShelvesetCreated;
                _autoShelve.OnTfsConnectionError += autoShelve_OnTfsConnectionError;
            }
            if (_options != null)
            {
                _options.OnOptionsChanged += Options_OnOptionsChanged;
            }
        }

        private void DetachEvents()
        {
            if (_autoShelve != null)
            {
                _autoShelve.OnStop -= autoShelve_OnStop;
                _autoShelve.OnStart -= autoShelve_OnStart;
                _autoShelve.OnShelvesetCreated -= autoShelve_OnShelvesetCreated;
                _autoShelve.OnTfsConnectionError -= autoShelve_OnTfsConnectionError;
            }
            if (_options != null)
            {
                _options.OnOptionsChanged -= Options_OnOptionsChanged;
            }
        }

        private void Options_OnOptionsChanged(object sender, OptionsChangedEventArgs e)
        {
            if (_autoShelve != null)
            {
                _autoShelve.MaximumShelvesets = e.MaximumShelvesets;
                _autoShelve.ShelvesetName = e.ShelvesetName;
                _autoShelve.TimerInterval = e.Interval;
            }
        }

        private void ToggleMenuCommandRunStateText(object sender)
        {
            try
            {
                var menuCommand = sender as OleMenuCommand;
                if (menuCommand != null)
                {
                    if (menuCommand.CommandID.Guid == GuidList.GuidAutoShelveCmdSet)
                    {
                        menuCommand.Text = _autoShelve.IsRunning ? _menuTextRunning : _menuTextStopped;
                    }
                }
            }
            catch { 
                // swallow exceptions 
            }
        }

        public void WriteToActivityLog(string message, string stackTrace)
        {
            try
            {
                if (_log != null)
                    _log.LogEntry(3, "VsExtAutoShelvePackage", string.Format(CultureInfo.CurrentCulture, "Message: {0} Stack Trace: {1}", message, stackTrace));
            }
            catch
            {
                // swallow exceptions
            }
        }

        private void WriteException(Exception ex)
        {
            WriteToStatusBar(string.Concat(_extName, " encountered an error."));
            WriteLineToOutputWindow(ex.Message);
            WriteLineToOutputWindow(ex.StackTrace);
            WriteToActivityLog(ex.Message, ex.StackTrace);
        }

        private void WriteLineToOutputWindow(string outputText)
        {
            WriteToOutputWindow(outputText, true);
        }

        private void WriteToOutputWindow(string outputText, bool newLine = false)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_options.OutputPane))
                {
                    var oWindow = _dte.ToolWindows.OutputWindow.OutputWindowPanes.Item(_options.OutputPane);
                    oWindow.OutputString(outputText);
                    if (newLine)
                        oWindow.OutputString(System.Environment.NewLine);
                }
            }
            catch
            {
                // swallow exceptions
            }
        }

        private void WriteToStatusBar(string text)
        {
            try
            {
                _dte.StatusBar.Text = text;
            }
            catch
            {
                // swallow exceptions
            }
        }

        #endregion

        #region DebuggerEvents

        private void OnEnterRunMode(dbgEventReason Reason)
        {
            if (_options.PauseWhileDebugging && !_isPaused)
            {
                _isPaused = true;
                _autoShelve.Stop();
            }
        }

        private void OnEnterDesignMode(dbgEventReason Reason)
        {
            if (_isPaused)
            {
                _autoShelve.CreateShelveset();
                _autoShelve.Start();
                _isPaused = false;
            }
        }

        #endregion

        #region IVsSolutionEvents

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            if (_autoShelve != null && _autoShelve.IsRunning)
            {
                _autoShelve.CreateShelveset();
            }
            return 0;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) { return 0; }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) { return 0; }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            if (_autoShelve != null && !_autoShelve.IsRunning)
            {
                _autoShelve.Start();
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

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~VsExtAutoShelvePackage()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected override void Dispose(bool disposeManaged)
        {
            try
            {
                DetachEvents();

                if (_solutionService != null && _solutionEventsCookie != 0)
                {
                    _solutionService.UnadviseSolutionEvents(_solutionEventsCookie);
                    _solutionEventsCookie = 0;
                    _solutionService = null;
                }
            }
            catch
            {
                // swallow exceptions
            }
            base.Dispose(disposeManaged);
        }

        #endregion

    }
}