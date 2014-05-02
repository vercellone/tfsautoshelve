using System;

namespace VsExt.AutoShelve.EventArgs {
    public class ShelvesetCreatedEventArgs : System.EventArgs {
        public Exception ExecutionException { get; set; }

        public bool ExecutionSuccess {
            get {
                return (ExecutionException == null);
            }
        }

        public int ShelvesetChangeCount { get; set; }
        
        public int ShelvesetsPurgeCount { get; set; }

        public string ShelvesetName { get; set; }
    }
}