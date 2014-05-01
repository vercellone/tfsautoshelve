namespace VsExt.AutoShelve.EventArgs {
    public class OptionsChangedEventArgs : System.EventArgs {
        public int Interval { get; set; }
        public ushort MaximumShelvesets { get; set; }
        public string ShelvesetName { get; set; }
        public bool SuppressDialogs { get; set; }
    }
}