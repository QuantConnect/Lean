using System;
using System.IO;
using System.Reflection;

var directory = Directory.GetCurrentDirectory();
foreach (var file in Directory.GetFiles(Directory.GetParent(directory).FullName, "*.dll"))
{
    try
    {
        Assembly.LoadFrom(file.ToString());
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}
