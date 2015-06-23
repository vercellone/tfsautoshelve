using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("VsExt.AutoShelve")]
[assembly: AssemblyCompany("Jason Vercellone")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCopyright("2015")]
[assembly: AssemblyDescription("Automatically backup all workspace pending changes to the TFS server via a shelveset.")]
[assembly: AssemblyProduct("TFS Auto Shelve")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(false)]
//[assembly: CompilationRelaxations(8)]
[assembly: ComVisible(false)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: NeutralResourcesLanguage("en-US")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("4.2.0.0")]
[assembly: AssemblyFileVersion("4.2.0.0")]

[assembly: InternalsVisibleTo("VsExt.AutoShelve_IntegrationTests, PublicKey=00240000048000009400000006020000002400005253413100040000010001004384bdada0d14bf775ac485ec80645ad481714418b8a6fbc2d336e96bcd57dd0e7f980239d8fe5eccd4088a1d042ac1c7a482ef1415e40b2787152d2669a9407416f71961323bf5ed4ec30012dc18e0185ce623d35ec47a54f9c591a54e13ab71bd3a10b339cd4ebc4bc7977bd6103f953da37aff8d1436d56ee173c2e5782e6")]
[assembly: InternalsVisibleTo("VsExt.AutoShelve_UnitTests, PublicKey=00240000048000009400000006020000002400005253413100040000010001004384bdada0d14bf775ac485ec80645ad481714418b8a6fbc2d336e96bcd57dd0e7f980239d8fe5eccd4088a1d042ac1c7a482ef1415e40b2787152d2669a9407416f71961323bf5ed4ec30012dc18e0185ce623d35ec47a54f9c591a54e13ab71bd3a10b339cd4ebc4bc7977bd6103f953da37aff8d1436d56ee173c2e5782e6")]