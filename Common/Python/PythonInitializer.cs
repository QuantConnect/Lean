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
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Python.Runtime;
using QuantConnect.Logging;

namespace QuantConnect.Python
{
    /// <summary>
    /// Helper class for Python initialization
    /// </summary>
    public static class PythonInitializer
    {
        // Used to allow multiple Python unit and regression tests to be run in the same test run
        private static bool _isBeginAllowThreadsCalled;

        /// <summary>
        /// Initialize the Python.NET library
        /// </summary>
        public static void Initialize()
        {
            if (!_isBeginAllowThreadsCalled)
            {
                Log.Trace("PythonInitializer.Initialize(): start...");
                PythonEngine.Initialize();

                // required for multi-threading usage
                PythonEngine.BeginAllowThreads();

                _isBeginAllowThreadsCalled = true;
                Log.Trace("PythonInitializer.Initialize(): ended");
            }
        }

        /// <summary>
        /// Adds directories to the python path at runtime
        /// </summary>
        public static void AddPythonPaths(IEnumerable<string> paths)
        {
            if (_isBeginAllowThreadsCalled)
            {
                using (Py.GIL())
                {
                    var code = string.Join(";", paths.Select(s => $"sys.path.append('{s}')"));
                    PythonEngine.Exec($"import sys;{code}");
                }
            }
        }

        /// <summary>
        /// Adds the provided paths to the end of the PYTHONPATH environment variable, as well
        /// as the current working directory.
        /// </summary>
        /// <param name="extraDirectories">Additional paths to add to the end of PYTHONPATH</param>
        public static void SetPythonPathEnvironmentVariable(IEnumerable<string> extraDirectories = null)
        {
            // create new python path environment variable containing directories
            var pythonDirectories = new List<string>();

            // Don't include an empty environment variable in pythonPath, otherwise the PYTHONPATH
            // environment variable won't be used in the module import process
            var pythonPathEnvironmentVariable = Environment.GetEnvironmentVariable("PYTHONPATH");
            if (!string.IsNullOrEmpty(pythonPathEnvironmentVariable))
            {
                pythonDirectories.Add(pythonPathEnvironmentVariable);
            }

            // Since the order of the PYTHONPATH matters, let's add any new
            // entries to the end of the new environment variable to prevent
            // any potential Python standard library paths from being de-prioritized.
            if (extraDirectories != null)
            {
                pythonDirectories.AddRange(extraDirectories);
            }

            // Add current directory too, allows python algorithms to find Lean's pre-defined submodules
            pythonDirectories.Add(new DirectoryInfo(Environment.CurrentDirectory).FullName);
            var finalPath = string.Join(OS.IsLinux ? ":" : ";", pythonDirectories.Distinct());

            Environment.SetEnvironmentVariable("PYTHONPATH", finalPath);
        }
    }
}
