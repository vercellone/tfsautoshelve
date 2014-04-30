using System;

namespace VsExt.AutoShelve {
    public class WorkSpaceDiscoveryEventArgs : EventArgs {
        public bool IsWorkspaceDiscovered { get; set; }

        public WorkSpaceDiscoveryEventArgs() {}
    }
}