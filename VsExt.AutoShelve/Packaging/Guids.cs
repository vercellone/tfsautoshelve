// Guids.cs
// MUST match guids.h
using System;

namespace VsExt.AutoShelve
{
    static class GuidList
    {
        public const string guidAutoShelvePkgString = "8fdc6155-e4ab-431d-a64f-6abb7b9e2cf1";
        public const string guidAutoShelveCmdSetString = "bcdacbea-1eef-437f-af26-bf6a45774688";

        public static readonly Guid guidAutoShelveCmdSet = new Guid(guidAutoShelveCmdSetString);
    };
}