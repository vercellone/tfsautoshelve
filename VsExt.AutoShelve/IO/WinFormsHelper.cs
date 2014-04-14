using System.Windows.Forms;

namespace VsExt.AutoShelve {
    public static class WinFormsHelper {

        private static DialogResult _result;
        private static bool _allowMsgBox;

        public static bool AllowMessageBox {
            get {
                return _allowMsgBox;
            }
            set {
                _allowMsgBox = value;
            }
        }

        public static DialogResult FakeDialogResult {
            get {
                return _result;
            }
            set {
                _result = value;
            }
        }

        static WinFormsHelper() {
            _result = DialogResult.None;
            _allowMsgBox = true;
        }

        public static DialogResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) {
            if (_allowMsgBox) {
                if (!string.IsNullOrWhiteSpace(text)) {
                    return MessageBox.Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
                }
            }
            return _result;
        }
    }
}