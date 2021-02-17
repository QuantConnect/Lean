using System.Reflection;
using System.Runtime.CompilerServices;

// common assembly attributes
[assembly: AssemblyCopyright("QuantConnect™ 2018. All Rights Reserved")]
[assembly: AssemblyCompany("QuantConnect Corporation")]
[assembly: AssemblyVersion("2.4")]

[assembly: InternalsVisibleTo("Xposure.Brokerages")]
[assembly: InternalsVisibleTo("Xposure.Lean.Engine")]
// Configuration used to build the assembly is by defaulting 'Debug'.
// To create a package using a Release configuration, -properties Configuration=Release on the command line must be use.
// source: https://docs.microsoft.com/en-us/nuget/reference/nuspec#replacement-tokens