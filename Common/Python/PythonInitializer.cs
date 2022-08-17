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
using System.IO;
using System.Linq;
using Python.Runtime;
using QuantConnect.Util;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace QuantConnect.Python
{
    /// <summary>
    /// Helper class for Python initialization
    /// </summary>
    public static class PythonInitializer
    {
        // Used to allow multiple Python unit and regression tests to be run in the same test run
        private static bool _isInitialized;

        // Used to hold pending path additions before Initialize is called
        private static List<string> _pendingPathAdditions = new List<string>();

        /// <summary>
        /// Initialize the Python.NET library
        /// </summary>
        public static void Initialize()
        {
            if (!_isInitialized)
            {
                Log.Trace("PythonInitializer.Initialize(): start...");
                PythonEngine.Initialize();

                // required for multi-threading usage
                PythonEngine.BeginAllowThreads();

                _isInitialized = true;
                Log.Trace("PythonInitializer.Initialize(): ended");

                AddPythonPaths(new []{ Environment.CurrentDirectory });
            }
        }

        /// <summary>
        /// Adds directories to the python path at runtime
        /// </summary>
        public static bool AddPythonPaths(IEnumerable<string> paths)
        {
            // Filter out any paths that are already on our Python path
            if (paths.IsNullOrEmpty())
            {
                return false;
            }

            // Add these paths to our pending additions
            _pendingPathAdditions.AddRange(paths);

            if (_isInitialized)
            {
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    using var locals = new PyDict();
                    locals.SetItem("sys", sys);

                    // Filter out any already paths that already exist on our current PythonPath
                    using var pythonCurrentPath = PythonEngine.Eval("sys.path", locals: locals);
                    var currentPath = pythonCurrentPath.As<List<string>>();
                    _pendingPathAdditions = _pendingPathAdditions.Where(x => !currentPath.Contains(x.Replace('\\', '/'))).ToList();

                    // Insert any pending path additions
                    if (!_pendingPathAdditions.IsNullOrEmpty())
                    {
                        var code = string.Join(";", _pendingPathAdditions
                            .Select(s => $"sys.path.insert(0, '{s}')")).Replace('\\', '/');
                        PythonEngine.Exec(code, locals: locals);

                        _pendingPathAdditions.Clear();
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// "Activate" a virtual Python environment by prepending its library storage to Pythons
        /// path. This allows the libraries in this venv to be selected prior to our base install.
        /// Requires PYTHONNET_PYDLL to be set to base install.
        /// </summary>
        /// <remarks>If a module is already loaded, Python will use its cached version first
        /// these modules must be reloaded by reload() from importlib library</remarks>
        public static bool ActivatePythonVirtualEnvironment(string pathToVirtualEnv)
        {
            if (string.IsNullOrEmpty(pathToVirtualEnv))
            {
                return false;
            }

            if(!Directory.Exists(pathToVirtualEnv))
            {
                Log.Error($"PythonIntializer.ActivatePythonVirtualEnvironment(): Path {pathToVirtualEnv} to virtual environment does not exist");
                return false;
            }

            pathToVirtualEnv = pathToVirtualEnv.TrimEnd('/', '\\');
            var pathsToPrepend = new List<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // For linux we need to know the python version to determine the lib folder containing our packages
                // Compare our PyDLL to the directory names under the lib directory and get a match
                var pyDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
                var version = Path.GetFileNameWithoutExtension(pyDll);
                var libDir = Directory.GetDirectories($"{pathToVirtualEnv}/lib")
                    .Select(d => new DirectoryInfo(d).Name)
                    .First(x => version.Contains(x, StringComparison.InvariantCulture));

                pathsToPrepend.Add($"{pathToVirtualEnv}/lib/{libDir}");
                pathsToPrepend.Add($"{pathToVirtualEnv}/lib/{libDir}/site-packages");
            }
            else
            {
                pathsToPrepend.Add($"{pathToVirtualEnv}\\Lib");
                pathsToPrepend.Add($"{pathToVirtualEnv}\\Lib\\site-packages");
            }

            Log.Trace($"PythonIntializer.ActivatePythonVirtualEnvironment(): Adding the following locations to Python Path: {string.Join(",", pathsToPrepend)}");

            return AddPythonPaths(pathsToPrepend);
        }
    }
}
