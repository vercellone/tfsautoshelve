using System;

namespace VsExt.AutoShelve.Packaging
{
    static class GuidList
    {
        public const string GuidAutoShelvePkgString = "8fdc6155-e4ab-431d-a64f-6abb7b9e2cf1";
        private const string GuidAutoShelveCmdSetString = "bcdacbea-1eef-437f-af26-bf6a45774688";

        public static readonly Guid GuidAutoShelveCmdSet = new Guid(GuidAutoShelveCmdSetString);
    };
}