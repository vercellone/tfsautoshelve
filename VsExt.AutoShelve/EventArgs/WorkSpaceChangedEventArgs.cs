using System;

namespace VsExt.AutoShelve {
    public class WorkspaceChangedEventArgs : EventArgs {
        public bool IsWorkspaceValid { get; set; }

        public WorkspaceChangedEventArgs() {}
    }
}