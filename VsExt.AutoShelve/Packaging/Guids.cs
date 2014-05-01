using System;

namespace VsExt.AutoShelve.Packaging
{
    static class GuidList
    {
        public const string GuidAutoShelvePkgString = "f293c726-f343-47ff-93c9-3d3469f5c373";
        private const string GuidAutoShelveCmdSetString = "2a9cce30-43ab-404f-90b4-fcea90504e4d";

        public static readonly Guid GuidAutoShelveCmdSet = new Guid(GuidAutoShelveCmdSetString);
    };
}