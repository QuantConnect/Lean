using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("QuantConnect.Common")]
//[assembly: AssemblyDescription("")]
//[assembly: AssemblyConfiguration("")]
//[assembly: AssemblyCompany("")]
//[assembly: AssemblyProduct("QuantConnect.Common")]
//[assembly: AssemblyCopyright("Copyright ©  2015")]
//[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("94687ba0-0b5f-43f7-a911-83b5a89651cf")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
//[assembly: AssemblyVersion("1.0.0.0")]
//[assembly: AssemblyFileVersion("1.0.0.0")]

// some things we want to expose to other parts of the engine but to not allow
// algorithms to have access. this certainly isn't the ideal, but we'd like
// to not compile break existing user algorithm's, but instead allow them to
// throw the exception in a backtest and see how to use the new order system.
[assembly: InternalsVisibleTo("QuantConnect.Algorithm.Framework")]
[assembly: InternalsVisibleTo("QuantConnect.Brokerages")]
[assembly: InternalsVisibleTo("QuantConnect.Lean.Engine")]
[assembly: InternalsVisibleTo("QuantConnect.Tests")]
