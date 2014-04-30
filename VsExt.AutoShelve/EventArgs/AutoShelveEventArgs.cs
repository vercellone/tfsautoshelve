using System;

namespace VsExt.AutoShelve {
    public class AutoShelveEventArgs : EventArgs {
        public Exception ExecutionException { get; set; }

        public bool ExecutionSuccess {
            get {
                return (this.ExecutionException == null);
            }
        }

        public int ShelvesetCount { get; set; }

        public string ShelvesetName { get; set; }

        public AutoShelveEventArgs() { }
    }
}