using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace VsExt.AutoShelve {

    [ComVisible(true)]
    [Guid("0f98bfc6-8c54-426a-94f5-256df616a90a")]
    public class OptionsPageGeneral : DialogPage {
        
        #region Properties

        private string _name;

        [Category("Auto Shelve Settings")]
        [Description("Shelve set name used as a string.Format input value where {0}=WorkspaceInfo.Name, {1}=WorkspaceInfo.OwnerName, {2}=DateTime.Now.  Anything greater than 64 characters will be truncated!")]
        public string ShelveSetName {
            get {
                return _name;
            }
            set {
                if (value.Length >= 30) {
                    WinFormsHelper.ShowMessageBox("Name will be truncated to 64 characters.", "Warning - TFS Auto Shelve Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                _name = value;
            }
        }

        private int _interval;

        [Category("Auto Shelve Settings")]
        [Description("The interval (in minutes) between shelvesets when running.")]
        public int TimerSaveInterval {
            get {
                return _interval;
            }
            set {
                if (value <=0) {
                    WinFormsHelper.ShowMessageBox("TimerSaveInterval must be greater than 0.", "Error - TFS Auto Shelve Settings", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                } else {
                    _interval = value;
                }
            }
        }

        #endregion

        public OptionsPageGeneral() {
            _name = "Auto-{1}";
            _interval = 5;
        }

        protected override void OnApply(DialogPage.PageApplyEventArgs e) {
            base.OnApply(e);
            bool flag = OnOptionsChanged == null;
            if (!flag) {
                OptionsEventArgs optionsEventArg = new OptionsEventArgs();
                optionsEventArg.ShelveSetName = ShelveSetName;
                optionsEventArg.Interval = TimerSaveInterval;
                OptionsEventArgs optionsEventArg1 = optionsEventArg;
                OnOptionsChanged(this, optionsEventArg1);
            }
        }

        public event EventHandler<OptionsEventArgs> OnOptionsChanged;
    }
}