using System;

namespace VsExt.AutoShelve {
    public class OptionsEventArgs : EventArgs {
        public string ShelveSetName { get; set; }

        public int Interval { get; set; }

        public OptionsEventArgs() {}
    }
}