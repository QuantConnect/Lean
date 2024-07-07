using System;
using System.IO;
using System.Linq;
using System.Reflection;

var currentDirectory = Directory.GetCurrentDirectory();
var parentDirectory = Directory.GetParent(currentDirectory).FullName;

// If our parent directory contains QC Dlls use it, otherwise default to current working directory
// In cloud and CLI research cases we expect the parent directory to contain the Dlls; but locally it may be cwd
var directoryToLoad = Directory.GetFiles(parentDirectory, "QuantConnect.*.dll").Any()
    ? parentDirectory
    : currentDirectory;

// Load in all QC dll's from this directory
Console.WriteLine($"Initialize.csx: Loading assemblies from {directoryToLoad}");
foreach (var file in Directory.GetFiles(directoryToLoad, "QuantConnect.*.dll"))
{
    try
    {
        Assembly.LoadFrom(file.ToString());
    }
    catch (Exception e)
    {
        Console.WriteLine($"File: {file}. Exception: {e}");
    }
}
