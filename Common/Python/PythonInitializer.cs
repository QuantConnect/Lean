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
using System.Linq;
using Python.Runtime;
using QuantConnect.Logging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using QuantConnect.Util;

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
        private static List<string> _pendingStartOfPathAdditions = new List<string>();
        private static List<string> _pendingEndOfPathAdditions = new List<string>();
        private static List<string> pathCache = new List<string>();

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
        public static void AddPythonPaths(IEnumerable<string> paths, bool prepend = false)
        {
            // Filter out any paths that are already on our Python path
            paths = paths?.Where(x => !pathCache.Contains(x.Replace('\\', '/'))).ToList();
            if (paths == null || !paths.Any())
            {
                return;
            }

            // Add these paths to our pending additions
            if (prepend)
            {
                _pendingStartOfPathAdditions.AddRange(paths);
            }
            else
            {
                _pendingEndOfPathAdditions.AddRange(paths);
            }

            if (_isInitialized)
            {
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    var locals = new PyDict();
                    locals.SetItem("sys", sys);

                    // Insert any pending start of path additions
                    if (!_pendingStartOfPathAdditions.IsNullOrEmpty())
                    {
                        PythonEngine.Exec(string.Join(";", _pendingStartOfPathAdditions.Select(s => $"sys.path.insert(0, '{s}')"))
                            .Replace('\\', '/'), null, locals.Handle);

                        _pendingStartOfPathAdditions.Clear();
                    }

                    // Append any end of path additions
                    if (!_pendingEndOfPathAdditions.IsNullOrEmpty())
                    {
                        PythonEngine.Exec(string.Join(";", _pendingEndOfPathAdditions.Select(s => $"sys.path.append('{s}')"))
                            .Replace('\\', '/'), null, locals.Handle);

                        _pendingEndOfPathAdditions.Clear();
                    }

                    // Update our path cache
                    pathCache = PythonEngine.Eval("sys.path", null, locals.Handle).As<List<string>>();
                    locals.Dispose();
                }
            }
        }

        /// <summary>
        /// "Activate" a virtual Python environment by prepending its library storage to PythonNets
        /// path. This allows the libraries in this venv to be selected prior to our base install.
        /// Requires PYTHONNET_PYDLL to be set to base install.
        /// </summary>
        /// <remarks>If a module is already loaded, Python will use its cached version first
        /// these modules must be reloaded by reload() from importlib library</remarks>
        public static void ActivatePythonVirtualEnvironment(string pathToVirtualEnv)
        {
            if (pathToVirtualEnv != null)
            {
                var pathsToPrepend = new List<string>();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    pathsToPrepend.Add($"{pathToVirtualEnv}/lib/python3.6");
                    pathsToPrepend.Add($"{pathToVirtualEnv}/lib/python3.6/site-packages");
                }
                else
                {
                    pathsToPrepend.Add($"{pathToVirtualEnv}\\Lib");
                    pathsToPrepend.Add($"{pathToVirtualEnv}\\Lib\\site-packages");
                }

                AddPythonPaths(pathsToPrepend, true);
            }
        }
    }
}
