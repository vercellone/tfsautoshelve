// Guids.cs
// MUST match guids.h
using System;

namespace VsExt.AutoShelve
{
    static class GuidList
    {
        public const string guidAutoShelvePkgString = "f293c726-f343-47ff-93c9-3d3469f5c373";
        public const string guidAutoShelveCmdSetString = "2a9cce30-43ab-404f-90b4-fcea90504e4d";

        public static readonly Guid guidAutoShelveCmdSet = new Guid(guidAutoShelveCmdSetString);
    };
}