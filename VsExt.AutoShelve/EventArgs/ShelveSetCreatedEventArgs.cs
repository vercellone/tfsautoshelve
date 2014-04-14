using System;

namespace VsExt.AutoShelve {
    public class ShelvesetCreatedEventArgs : EventArgs {
        public Exception ExecutionException { get; set; }

        public bool ExecutionSuccess {
            get {
                return (this.ExecutionException == null);
            }
        }

        public int ShelvesetChangeCount { get; set; }
        
        public int ShelvesetsPurgeCount { get; set; }

        public string ShelvesetName { get; set; }

        public ShelvesetCreatedEventArgs() { }
    }
}