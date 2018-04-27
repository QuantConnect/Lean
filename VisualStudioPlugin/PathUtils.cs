/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using EnvDTE80;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Collections of util methods to work with directories
    /// </summary>
    internal static class PathUtils
    {
        private static readonly Dictionary<string, Language> _extensionsDictionary = new Dictionary<string, Language>();

        static PathUtils()
        {
            _extensionsDictionary[".cs"] = Language.CSharp;
            _extensionsDictionary[".fs"] = Language.FSharp;
            _extensionsDictionary[".py"] = Language.Python;
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
    }
}
