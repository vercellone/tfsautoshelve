using System;

namespace VsExt.AutoShelve.EventArgs {
    public class TfsConnectionErrorEventArgs : System.EventArgs
    {
        public Exception ConnectionError { get; set; }
    }
}