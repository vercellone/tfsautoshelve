using System.Globalization;
using Microsoft.VisualStudio.Shell.Interop;

namespace VsExt.AutoShelve {
    public class ActivityLog {

        private static IVsActivityLog _log;

        public static IVsActivityLog log {
            get {
                return _log;
            }
            set {
                _log = value;
            }
        }

        private ActivityLog() {}

        public static void WriteToActivityLog(string message, string stackTrace) {
            if (log != null) {
                log.LogEntry(3, "VsExtAutoShelvePackage", string.Format(CultureInfo.CurrentCulture, "Message: {0} Stack Trace: {1}", message, stackTrace));
            }
        }
    }
}