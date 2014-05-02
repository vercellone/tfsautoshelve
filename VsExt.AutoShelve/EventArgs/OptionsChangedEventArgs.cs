using System;

namespace VsExt.AutoShelve {
    public class OptionsChangedEventArgs : EventArgs {
        public int Interval { get; set; }
        public ushort MaximumShelvesets { get; set; }
        public string OutputPane { get; set; }
        public string ShelvesetName { get; set; }
        public bool SuppressDialogs { get; set; }
        public OptionsChangedEventArgs() {}
    }
}