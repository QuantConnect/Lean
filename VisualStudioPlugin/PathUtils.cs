using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Collections of util methods to work with directories
    /// </summary>
    static class PathUtils
    {

        private static Dictionary<string, Language> _extensionsDictionary = new Dictionary<string, Language>();

        static PathUtils()
        {
            _extensionsDictionary[".cs"] = Language.CSharp;
            _extensionsDictionary[".java"] = Language.Java;
            _extensionsDictionary[".vb"] = Language.VisualBasic;
            _extensionsDictionary[".fs"] = Language.FSharp;
        }

        public static string GetSolutionFolder(DTE2 dte2)
        {
            return Path.GetDirectoryName(dte2.Solution.FullName);
        }

        public static Language? DetermineProjectLanguage(List<string> filePaths)
        {

            var extensionsSet = new HashSet<string>();
            foreach (var filePath in filePaths)
            {
                extensionsSet.Add(Path.GetExtension(filePath));
            }

            if (extensionsSet.Count == 1 && _extensionsDictionary.ContainsKey(extensionsSet.First()))
            {
                return _extensionsDictionary[extensionsSet.First()];
            }

            return null;
        }
    }
}
