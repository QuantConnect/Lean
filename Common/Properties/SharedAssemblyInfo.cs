using System.Reflection;

// common assembly attributes
[assembly: AssemblyDescription("Part of the QuantConnect product.")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyCompany("QuantConnect")]
[assembly: AssemblyTrademark("QuantConnect")]
[assembly: AssemblyVersion("2.1.0.1")]
#if DEBUG
    [assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration("Release")]
#endif