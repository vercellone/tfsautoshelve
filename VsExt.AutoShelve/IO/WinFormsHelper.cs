using System.Windows.Forms;

namespace VsExt.AutoShelve.IO {
    public static class WinFormsHelper {
        public static void ShowMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            MessageBox.Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
        }
    }
}