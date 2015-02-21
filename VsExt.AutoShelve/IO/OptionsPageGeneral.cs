using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using VsExt.AutoShelve.EventArgs;

namespace VsExt.AutoShelve.IO {

    [ComVisible(true)]
    [Guid("0f98bfc6-8c54-426a-94f5-256df616a90a")]
    public class OptionsPageGeneral : DialogPage {
        private const string GeneralCat = "General";

        #region Properties

        [Category(GeneralCat), DisplayName(@"Shelveset Name"), Description("Shelve set name used as a string.Format input value where {0}=WorkspaceInfo.Name, {1}=WorkspaceInfo.OwnerName, {2}=DateTime.Now.  IMPORTANT: If you use multiple workspaces, and don't include WorkspaceInfo.Name then only the pending changes in the last workspace will be included in the shelveset. Anything greater than 64 characters will be truncated!")]
        public string ShelvesetName { get; set; }

        private int _interval;

        [Category(GeneralCat)]
        [DisplayName(@"Interval")]
        [Description("The interval (in minutes) between shelvesets when running.")]
        public int TimerSaveInterval {
            get {
                return _interval;
            }
            set {
                if (value <= 0) {
                    WinFormsHelper.ShowMessageBox("TimerSaveInterval must be greater than 0.", string.Format("Error - {0} Settings", Resources.ExtensionName), MessageBoxButtons.OK, MessageBoxIcon.Hand);
                } else {
                    _interval = value;
                }
            }
        }

        [Category(GeneralCat), DisplayName(@"Suppress Dialogs"), Description("Suppress run time dialogs.  Currently just the nagging 'Please connect to a Team Project first.' MessageBox")]
        public bool SuppressDialogs { get; set; }

        [Category(GeneralCat), DisplayName(@"Output Pane"), Description("Output window pane to write status messages.  If you set this to an empty string, nothing is written to the Output window.  Note: Regardless, the output pane is no longer explicitly activated.  So, no more focus stealing!")]
        public string OutputPane { get; set; }

        [Category(GeneralCat), DisplayName(@"Μaximum Shelvesets"), Description("Maximum number of shelvesets to retain.  Older shelvesets will be deleted. 0=Disabled. Note: ShelvesetName must include a {2} (DateTime.Now component) unique enough to generate more than the maximum for this to have any impact.  If {0} (WorkspaceInfo.Name) is included, then the max is applied per workspace.")]
        public ushort MaximumShelvesets { get; set; }

        #endregion

        public OptionsPageGeneral() {
            OutputPane = Resources.ExtensionName;
            MaximumShelvesets = 0;
            ShelvesetName = "Auto {0}";
            TimerSaveInterval = 5;
            SuppressDialogs = true;
        }

        protected override void OnApply(PageApplyEventArgs e) {
            base.OnApply(e);
            bool flag = OnOptionsChanged == null;
            if (!flag) {
                var optionsEventArg = new OptionsChangedEventArgs
                {
                    Interval = TimerSaveInterval,
                    MaximumShelvesets = MaximumShelvesets,
                	OutputPane = OutputPane,
                    ShelvesetName = ShelvesetName,
                    SuppressDialogs = SuppressDialogs
                };
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