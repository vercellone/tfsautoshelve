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

        [Category(GeneralCat), DisplayName(@"Pause while Debugging"), Description("If True, Auto Shelve will pause while debugging")]
        public bool PauseWhileDebugging { get; set; }

        private string _shelveSetName;
        [Category(GeneralCat), DisplayName(@"Shelveset Name"), Description("Shelve set name used as a string.Format input value where {0}=WorkspaceInfo.Name, {1}=WorkspaceInfo.OwnerName, {2}=DateTime.Now, {3}=Domain of WorkspaceInfo.OwnerName, {4}=UserName of WorkspaceInfo.OwnerName.  IMPORTANT: If you use multiple workspaces, and don't include WorkspaceInfo.Name then only the pending changes in the last workspace will be included in the shelveset. Anything greater than 64 characters will be truncated!")]
        public string ShelvesetName { 
            get { 
                if (String.IsNullOrWhiteSpace(_shelveSetName)) {
                    _shelveSetName = "Auto {0}";
                }
                return _shelveSetName;
            }
            set {
                _shelveSetName = value;
            } 
        }

        private double _interval;

        [Category(GeneralCat)]
        [DisplayName(@"Interval")]
        [Description("The interval (in minutes) between shelvesets when running.")]
        public double TimerSaveInterval {
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
            PauseWhileDebugging = false;
        }

        protected override void OnApply(PageApplyEventArgs e) {
            base.OnApply(e);
            bool flag = OnOptionsChanged == null;
            if (!flag) {
                var optionsEventArg = new OptionsChangedEventArgs
                {
                    PauseWhileDebugging = PauseWhileDebugging,
                    Interval = TimerSaveInterval,
                    MaximumShelvesets = MaximumShelvesets,
                	OutputPane = OutputPane,
                    ShelvesetName = ShelvesetName,
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