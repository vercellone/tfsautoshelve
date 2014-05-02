using System.Globalization;
using Microsoft.VisualStudio.Shell.Interop;

namespace VsExt.AutoShelve.IO {
    public static class ActivityLog {
        public static IVsActivityLog Log { get; set; }

        public static void WriteToActivityLog(string message, string stackTrace) {
            if (Log != null) {
                Log.LogEntry(3, "VsExtAutoShelvePackage", string.Format(CultureInfo.CurrentCulture, "Message: {0} Stack Trace: {1}", message, stackTrace));
            }
        }
    }
}