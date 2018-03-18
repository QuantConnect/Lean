using EnvDTE80;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        /// <summary>
        /// Get path to the currently opened solution folder
        /// </summary>
        /// <param name="dte2">VisualStudio DTE2 instance</param>
        /// <returns>Path to the currently opened solution folder</returns>
        public static string GetSolutionFolder(DTE2 dte2)
        {
            return Path.GetDirectoryName(dte2.Solution.FullName);
        }

        /// <summary>
        /// Determine programming language from a set of selected files
        /// </summary>
        /// <param name="filePaths">List of files in a project</param>
        /// <returns>Programming language if it can be determined, null otherwise</returns>
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

        /// <summary>
        /// Validate if a provided path is a valid data folder path
        /// </summary>
        /// <param name="dataFolderPath">Path to a data folder</param>
        /// <returns>True if this is a valid data folder path, false otherwise</returns>
        public static bool DataFolderPathValid(string dataFolderPath)
        {
            return Directory.Exists(dataFolderPath);
        }
    }
}
