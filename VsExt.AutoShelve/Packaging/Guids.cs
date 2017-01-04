using System;

namespace VsExt.AutoShelve.Packaging
{
    static class GuidList
    {
        public const string GuidAutoShelvePkgString = "8016DBDE-8330-4802-9B1C-1E0AD1102A24";
        private const string GuidAutoShelveCmdSetString = "4375B001-B852-4FA2-BD87-3106ACECDBBF";

        public static readonly Guid GuidAutoShelveCmdSet = new Guid(GuidAutoShelveCmdSetString);
    };
}