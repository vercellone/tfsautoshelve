using System;

namespace VsExt.AutoShelve.Packaging
{
    static class GuidList
    {
        public const string GuidAutoShelvePkgString = "E2FEEB86-AEB7-4ABB-A3BA-88457F4F7F27";
        private const string GuidAutoShelveCmdSetString = "4375B001-B852-4FA2-BD87-3106ACECDBBF";

        public static readonly Guid GuidAutoShelveCmdSet = new Guid(GuidAutoShelveCmdSetString);
    };
}