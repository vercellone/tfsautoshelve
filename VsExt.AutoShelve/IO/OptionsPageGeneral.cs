using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace VsExt.AutoShelve {

    [ComVisible(true)]
    [Guid("0f98bfc6-8c54-426a-94f5-256df616a90a")]
    public class OptionsPageGeneral : DialogPage {

        protected const string GENERAL_CAT = "General";

        #region Properties

        private string _name;

        [Category(GENERAL_CAT)]
        [DisplayName("Shelveset Name")]
        [Description("Shelve set name used as a string.Format input value where {0}=WorkspaceInfo.Name, {1}=WorkspaceInfo.OwnerName, {2}=DateTime.Now.  IMPORTANT: If you use multiple workspaces, and don't include WorkspaceInfo.Name then only the pending changes in the last workspace will be included in the shelveset. Anything greater than 64 characters will be truncated!")]
        public string ShelvesetName {
            get {
                return _name;
            }
            set {
                _name = value;
            }
        }

        private int _interval;

        [Category(GENERAL_CAT)]
        [DisplayName("Interval")]
        [Description("The interval (in minutes) between shelvesets when running.")]
        public int TimerSaveInterval {
            get {
                return _interval;
            }
            set {
                if (value <= 0) {
                    WinFormsHelper.ShowMessageBox("TimerSaveInterval must be greater than 0.", "Error - TFS Auto Shelve Settings", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                } else {
                    _interval = value;
                }
            }
        }

        private bool _suppressDialogs;

        [Category(GENERAL_CAT)]
        [DisplayName("Suppress Dialogs")]
        [Description("Suppress run time dialogs.  Currently just the nagging 'Please connect to a Team Project first.' MessageBox")]
        public bool SuppressDialogs {
            get {
                return _suppressDialogs;
            }
            set {
                _suppressDialogs = value;
            }
        }

        [Category(GENERAL_CAT), DisplayName(@"Output Pane"), Description("Output window pane to write status messages.  If you set this to an empty string, nothing is written to the Output window.  Note: Regardless, the output pane is no longer explicitly activated.  So, no more focus stealing!")]
        public string OutputPane { get; set; }

        private ushort _maxSets;

        [Category(GENERAL_CAT)]
        [DisplayName("Μaximum Shelvesets")]
        [Description("Maximum number of shelvesets to retain.  Older shelvesets will be deleted. 0=Disabled. Note: ShelvesetName must include a {2} (DateTime.Now component) unique enough to generate more than the maximum for this to have any impact.  If {0} (WorkspaceInfo.Name) is included, then the max is applied per workspace.")]
        public ushort MaximumShelvesets
        {
            get {
                return _maxSets;
            }
            set {
                _maxSets = value;
            }
        }

        #endregion

        public OptionsPageGeneral() {
            OutputPane = "TFS Auto Shelve";
            _maxSets = 0;
            _name = "Auto {0}";
            _interval = 5;
            _suppressDialogs = true;
        }

        protected override void OnApply(DialogPage.PageApplyEventArgs e) {
            base.OnApply(e);
            bool flag = OnOptionsChanged == null;
            if (!flag) {
                OptionsChangedEventArgs optionsEventArg = new OptionsChangedEventArgs();
                optionsEventArg.Interval = TimerSaveInterval;
                optionsEventArg.MaximumShelvesets = MaximumShelvesets;
                optionsEventArg.OutputPane = OutputPane;
                optionsEventArg.ShelvesetName = ShelvesetName;
                optionsEventArg.SuppressDialogs = SuppressDialogs;
                OnOptionsChanged(this, optionsEventArg);
            }
        }

        public event EventHandler<OptionsChangedEventArgs> OnOptionsChanged;

        //protected virtual IWin32Window Window {
        //    get {
        //        return new OptionsToolWindow().Window;
        //    }
        //}

     }
}