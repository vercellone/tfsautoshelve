using System;

namespace VsExt.AutoShelve.Packaging
{
    static class GuidList
    {
        public const string GuidAutoShelvePkgString = "BAEA1B9D-EDFE-41CB-A233-F9453BBAC7DA";
        private const string GuidAutoShelveCmdSetString = "E06C3308-36F4-417E-8845-CF32B80D0FB5";

        public static readonly Guid GuidAutoShelveCmdSet = new Guid(GuidAutoShelveCmdSetString);
    };
}