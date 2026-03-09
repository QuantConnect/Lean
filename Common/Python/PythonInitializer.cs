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
using QuantConnect.Configuration;

namespace QuantConnect.Python
{
    /// <summary>
    /// Helper class for Python initialization
    /// </summary>
    public static class PythonInitializer
    {
        private static bool IncludeSystemPackages;
        private static string PathToVirtualEnv;

        // Used to allow multiple Python unit and regression tests to be run in the same test run
        private static bool _isInitialized;

        // Used to hold pending path additions before Initialize is called
        private static List<string> _pendingPathAdditions = new List<string>();

        private static string _algorithmLocation;

        /// <summary>
        /// Initialize python.
        ///
        /// In some cases, we might not need to call BeginAllowThreads, like when we're running
        /// in a python or non-threaded environment.
        /// In those cases, we can set the beginAllowThreads parameter to false.
        /// </summary>
        public static void Initialize(bool beginAllowThreads = true)
        {
            if (!_isInitialized)
            {
                Log.Trace($"PythonInitializer.Initialize(): {Messages.PythonInitializer.Start}...");
                PythonEngine.Initialize();

                if (beginAllowThreads)
                {
                    // required for multi-threading usage
                    PythonEngine.BeginAllowThreads();
                }

                _isInitialized = true;

                ConfigurePythonPaths();

                TryInitPythonVirtualEnvironment();
                Log.Trace($"PythonInitializer.Initialize(): {Messages.PythonInitializer.Ended}");
            }
        }

        /// <summary>
        /// Shutdown python
        /// </summary>
        public static void Shutdown()
        {
            if (_isInitialized)
            {
                Log.Trace($"PythonInitializer.Shutdown(): {Messages.PythonInitializer.Start}");
                _isInitialized = false;

                try
                {
                    var pyLock = Py.GIL();
                    PythonEngine.Shutdown();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                Log.Trace($"PythonInitializer.Shutdown(): {Messages.PythonInitializer.Ended}");
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
            _pendingPathAdditions.AddRange(paths.Where(x => !_pendingPathAdditions.Contains(x)));

            if (_isInitialized)
            {
                using (Py.GIL())
                {
                    using dynamic sys = Py.Import("sys");
                    using var locals = new PyDict();
                    locals.SetItem("sys", sys);

                    // Filter out any already paths that already exist on our current PythonPath
                    using var pythonCurrentPath = PythonEngine.Eval("sys.path", locals: locals);
                    var currentPath = pythonCurrentPath.As<List<string>>();
                    _pendingPathAdditions = _pendingPathAdditions.Where(x => !currentPath.Contains(x.Replace('\\', '/'))).ToList();

                    // Algorithm location most always be before any other path added through this method
                    var insertionIndex = 0;
                    if (!_algorithmLocation.IsNullOrEmpty())
                    {
                        insertionIndex = currentPath.IndexOf(_algorithmLocation.Replace('\\', '/')) + 1;

                        if (insertionIndex == 0)
                        {
                            // The algorithm location is not in the current path so it must be in the pending additions list.
                            // Let's move it to the back so it ends up added at the beginning of the path list
                            _pendingPathAdditions.Remove(_algorithmLocation);
                            _pendingPathAdditions.Add(_algorithmLocation);
                        }
                    }

                    // Insert any pending path additions
                    if (!_pendingPathAdditions.IsNullOrEmpty())
                    {
                        var code = string.Join(";", _pendingPathAdditions
                            .Select(s => $"sys.path.insert({insertionIndex}, '{s}')")).Replace('\\', '/');
                        PythonEngine.Exec(code, locals: locals);

                        _pendingPathAdditions.Clear();
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Adds the algorithm location to the python path.
        /// This will make sure that <see cref="AddPythonPaths" /> keeps the algorithm location path
        /// at the beginning of the pythonpath.
        /// </summary>
        public static void AddAlgorithmLocationPath(string algorithmLocation)
        {
            if (!_algorithmLocation.IsNullOrEmpty())
            {
                return;
            }

            if (!Directory.Exists(algorithmLocation))
            {
                Log.Error($@"PythonInitializer.AddAlgorithmLocationPath(): {
                    Messages.PythonInitializer.UnableToLocateAlgorithm(algorithmLocation)}");
                return;
            }

            _algorithmLocation = algorithmLocation;
            AddPythonPaths(new[] { _algorithmLocation });
        }

        /// <summary>
        /// Resets the algorithm location path so another can be set
        /// </summary>
        public static void ResetAlgorithmLocationPath()
        {
            _algorithmLocation = null;
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
                Log.Error($@"PythonIntializer.ActivatePythonVirtualEnvironment(): {
                    Messages.PythonInitializer.VirutalEnvironmentNotFound(pathToVirtualEnv)}");
                return false;
            }

            PathToVirtualEnv = pathToVirtualEnv;

            bool? includeSystemPackages = null;
            var configFile = new FileInfo(Path.Combine(PathToVirtualEnv, "pyvenv.cfg"));
            if(configFile.Exists)
            {
                foreach (var line in File.ReadAllLines(configFile.FullName))
                {
                    if (line.Contains("include-system-site-packages", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // format: include-system-site-packages = false (or true)
                        var equalsIndex = line.IndexOf('=', StringComparison.InvariantCultureIgnoreCase);
                        if(equalsIndex != -1 && line.Length > (equalsIndex + 1) && bool.TryParse(line.Substring(equalsIndex + 1).Trim(), out var result))
                        {
                            includeSystemPackages = result;
                            break;
                        }
                    }
                }
            }

            if(!includeSystemPackages.HasValue)
            {
                includeSystemPackages = true;
                Log.Error($@"PythonIntializer.ActivatePythonVirtualEnvironment(): {
                    Messages.PythonInitializer.FailedToFindSystemPackagesConfiguration(pathToVirtualEnv, configFile)}");
            }
            else
            {
                Log.Trace($@"PythonIntializer.ActivatePythonVirtualEnvironment(): {
                    Messages.PythonInitializer.SystemPackagesConfigurationFound(pathToVirtualEnv, includeSystemPackages.Value)}");
            }

            if (!includeSystemPackages.Value)
            {
                PythonEngine.SetNoSiteFlag();
            }

            IncludeSystemPackages = includeSystemPackages.Value;

            TryInitPythonVirtualEnvironment();
            return true;
        }

        private static void TryInitPythonVirtualEnvironment()
        {
            if (!_isInitialized || string.IsNullOrEmpty(PathToVirtualEnv))
            {
                return;
            }

            using (Py.GIL())
            {
                using dynamic sys = Py.Import("sys");
                using var locals = new PyDict();
                locals.SetItem("sys", sys);

                if (!IncludeSystemPackages)
                {
                    var currentPath = (List<string>)sys.path.As<List<string>>();
                    var toRemove = new List<string>(currentPath.Where(s => s.Contains("site-packages", StringComparison.InvariantCultureIgnoreCase)));
                    if (toRemove.Count > 0)
                    {
                        var code = string.Join(";", toRemove.Select(s => $"sys.path.remove('{s}')"));
                        PythonEngine.Exec(code, locals: locals);
                    }
                }

                // fix the prefixes to point to our venv
                sys.prefix = PathToVirtualEnv;
                sys.exec_prefix = PathToVirtualEnv;

                using dynamic site = Py.Import("site");
                // This has to be overwritten because site module may already have been loaded by the interpreter (but not run yet)
                site.PREFIXES = new List<PyObject> { sys.prefix, sys.exec_prefix };
                // Run site path modification with tweaked prefixes
                site.main();

                if (IncludeSystemPackages)
                {
                    // let's make sure our site packages is at the start so that we support overriding system libraries with a version in the env
                    PythonEngine.Exec(@$"if sys.path[-1].startswith('{PathToVirtualEnv}'):
    sys.path.insert(0, sys.path.pop())", locals: locals);
                }

                if (Log.DebuggingEnabled)
                {
                    using dynamic os = Py.Import("os");
                    var path = new List<string>();
                    foreach (var p in sys.path)
                    {
                        path.Add((string)p);
                    }

                    Log.Debug($"PythonIntializer.InitPythonVirtualEnvironment(): PYTHONHOME: {os.getenv("PYTHONHOME")}." +
                        $" PYTHONPATH: {os.getenv("PYTHONPATH")}." +
                        $" sys.executable: {sys.executable}." +
                        $" sys.prefix: {sys.prefix}." +
                        $" sys.base_prefix: {sys.base_prefix}." +
                        $" sys.exec_prefix: {sys.exec_prefix}." +
                        $" sys.base_exec_prefix: {sys.base_exec_prefix}." +
                        $" sys.path: [{string.Join(",", path)}]");
                }
            }
        }

        /// <summary>
        /// Gets the python additional paths from the config and adds them to Python using the PythonInitializer
        /// </summary>
        private static void ConfigurePythonPaths()
        {
            var pythonAdditionalPaths = new List<string> { Environment.CurrentDirectory };
            pythonAdditionalPaths.AddRange(Config.GetValue("python-additional-paths", Enumerable.Empty<string>()));
            AddPythonPaths(pythonAdditionalPaths.Where(path =>
            {
                var pathExists = Directory.Exists(path);
                if (!pathExists)
                {
                    Log.Error($"PythonInitializer.ConfigurePythonPaths(): {Messages.PythonInitializer.PythonPathNotFound(path)}");
                }

                return pathExists;
            }));
        }
    }
}
