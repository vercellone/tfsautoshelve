namespace VsExt.AutoShelve.EventArgs {
    public class OptionsChangedEventArgs : System.EventArgs {
        public bool PauseWhileDebugging { get; set; }
        public double Interval { get; set; }
        public ushort MaximumShelvesets { get; set; }
        public string OutputPane { get; set; }
        public string ShelvesetName { get; set; }
    }
}