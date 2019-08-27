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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.AlgorithmFactory.Python.Wrappers;
using QuantConnect.Configuration;
using QuantConnect.Python;
using QuantConnect.Util;

namespace QuantConnect.AlgorithmFactory
{
    /// <summary>
    /// Loader creates and manages the memory and exception space of the algorithm, ensuring if it explodes the Lean Engine is intact.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Loader : MarshalByRefObject
    {
        // True if we are in a debugging session
        private readonly bool _debugging;

        // Defines the maximum amount of time we will allow for instantiating an instance of IAlgorithm
        private readonly TimeSpan _loaderTimeLimit;

        // Language of the loader class.
        private readonly Language _language;

        // Defines how we resolve a list of type names into a single type name to be instantiated
        private readonly Func<List<string>, string> _multipleTypeNameResolverFunction;

        // The worker thread instance the loader will use if not null
        private readonly WorkerThread _workerThread;

        /// <summary>
        /// Memory space of the user algorithm
        /// </summary>
        public AppDomain appDomain;

        /// <summary>
        /// The algorithm's interface type that we'll be trying to load
        /// </summary>
        private static readonly Type AlgorithmInterfaceType = typeof (IAlgorithm);

        /// <summary>
        /// The full type name of QCAlgorithm, this is so we don't pick him up when querying for types
        /// </summary>
        private const string AlgorithmBaseTypeFullName = "QuantConnect.Algorithm.QCAlgorithm";

        /// <summary>
        /// The full type name of QCAlgorithmFramework, this is so we don't pick him up when querying for types
        /// </summary>
        private const string FrameworkBaseTypeFullName = "QuantConnect.Algorithm.Framework.QCAlgorithmFramework";

        /// <summary>
        /// Creates a new loader with a 10 second maximum load time that forces exactly one derived type to be found
        /// </summary>
        public Loader()
            : this(false, Language.CSharp, TimeSpan.FromSeconds(10), names => names.SingleOrDefault())
        {
        }

        /// <summary>
        /// Creates a new loader with the specified configuration
        /// </summary>
        /// <param name="debugging">True if we are debugging</param>
        /// <param name="language">Which language are we trying to load</param>
        /// <param name="loaderTimeLimit">
        /// Used to limit how long it takes to create a new instance
        /// </param>
        /// <param name="multipleTypeNameResolverFunction">
        /// Used to resolve multiple type names found in assembly to a single type name, if null, defaults to names => names.SingleOrDefault()
        ///
        /// When we search an assembly for derived types of IAlgorithm, sometimes the assembly will contain multiple matching types. This is the case
        /// for the QuantConnect.Algorithm assembly in this solution.  In order to pick the correct type, consumers must specify how to pick the type,
        /// that's what this function does, it picks the correct type from the list of types found within the assembly.
        /// </param>
        /// <param name="workerThread">The worker thread instance the loader should use</param>
        public Loader(bool debugging, Language language, TimeSpan loaderTimeLimit, Func<List<string>, string> multipleTypeNameResolverFunction, WorkerThread workerThread = null)
        {
            _debugging = debugging;
            _language = language;
            _workerThread = workerThread;
            if (multipleTypeNameResolverFunction == null)
            {
                throw new ArgumentNullException("multipleTypeNameResolverFunction");
            }

            _loaderTimeLimit = loaderTimeLimit;
            _multipleTypeNameResolverFunction = multipleTypeNameResolverFunction;
        }


        /// <summary>
        /// Creates a new instance of the specified class in the library, safely.
        /// </summary>
        /// <param name="assemblyPath">Location of the DLL</param>
        /// <param name="algorithmInstance">Output algorithm instance</param>
        /// <param name="errorMessage">Output error message on failure</param>
        /// <returns>Bool true on successfully loading the class.</returns>
        public bool TryCreateAlgorithmInstance(string assemblyPath, out IAlgorithm algorithmInstance, out string errorMessage)
        {
            //Default initialisation of Assembly.
            algorithmInstance = null;
            errorMessage = "";

            //First most basic check:
            if (!File.Exists(assemblyPath))
            {
                return false;
            }

            switch (_language)
            {
                case Language.Python:
                    TryCreatePythonAlgorithm(assemblyPath, out algorithmInstance, out errorMessage);
                    break;

                default:
                    TryCreateILAlgorithm(assemblyPath, out algorithmInstance, out errorMessage);
                    break;
            }

            //Successful load.
            return algorithmInstance != null;
        }

        /// <summary>
        /// Create a new instance of a python algorithm
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <param name="algorithmInstance"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private bool TryCreatePythonAlgorithm(string assemblyPath, out IAlgorithm algorithmInstance, out string errorMessage)
        {
            algorithmInstance = null;
            errorMessage = string.Empty;

            //File does not exist.
            if (!File.Exists(assemblyPath))
            {
                errorMessage = $"Loader.TryCreatePythonAlgorithm(): Unable to find py file: {assemblyPath}";
                return false;
            }

            var pythonFile = new FileInfo(assemblyPath);
            var moduleName = pythonFile.Name.Replace(".pyc", "").Replace(".py", "");

            try
            {
                PythonInitializer.Initialize();

                algorithmInstance = new AlgorithmPythonWrapper(moduleName);

                // we need stdout for debugging
                if (!_debugging && Config.GetBool("mute-python-library-logging", true))
                {
                    using (Py.GIL())
                    {
                        PythonEngine.Exec(
                            @"
import logging, os, sys
sys.stdout = open(os.devnull, 'w')
logging.captureWarnings(True)"
                        );
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                errorMessage = $"Loader.TryCreatePythonAlgorithm(): Unable to import python module {assemblyPath}. {e.Message}";
                return false;
            }

            //Successful load.
            return true;
        }

        /// <summary>
        /// Create a generic IL algorithm
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <param name="algorithmInstance"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private bool TryCreateILAlgorithm(string assemblyPath, out IAlgorithm algorithmInstance, out string errorMessage)
        {
            errorMessage = "";
            algorithmInstance = null;

            try
            {
                byte[] debugInformationBytes = null;

                // if the assembly is located in the base directory then don't bother loading the pdbs
                // manually, they'll be loaded automatically by the .NET runtime.
                var directoryName = new FileInfo(assemblyPath).DirectoryName;
                if (directoryName != null && directoryName.TrimEnd(Path.DirectorySeparatorChar) != AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))
                {
                    // see if the pdb exists
                    var mdbFilename = assemblyPath + ".mdb";
                    var pdbFilename = assemblyPath.Substring(0, assemblyPath.Length - 4) + ".pdb";
                    if (File.Exists(pdbFilename))
                    {
                        debugInformationBytes = File.ReadAllBytes(pdbFilename);
                    }
                    // see if the mdb exists
                    if (File.Exists(mdbFilename))
                    {
                        debugInformationBytes = File.ReadAllBytes(mdbFilename);
                    }
                }

                //Load the assembly:
                Assembly assembly;
                if (debugInformationBytes == null)
                {
                    Log.Trace("Loader.TryCreateILAlgorithm(): Loading only the algorithm assembly");
                    assembly = Assembly.LoadFrom(assemblyPath);
                }
                else
                {
                    Log.Trace("Loader.TryCreateILAlgorithm(): Loading debug information with algorithm");
                    var assemblyBytes = File.ReadAllBytes(assemblyPath);
                    assembly = Assembly.Load(assemblyBytes, debugInformationBytes);
                }

                //Get the list of extention classes in the library:
                var types = GetExtendedTypeNames(assembly);
                Log.Debug("Loader.TryCreateILAlgorithm(): Assembly types: " + string.Join(",", types));

                //No extensions, nothing to load.
                if (types.Count == 0)
                {
                    errorMessage = "Algorithm type was not found.";
                    Log.Error("Loader.TryCreateILAlgorithm(): Types array empty, no algorithm type found.");
                    return false;
                }

                if (types.Count > 1)
                {
                    // reshuffle type[0] to the resolved typename
                    types[0] = _multipleTypeNameResolverFunction.Invoke(types);

                    if (string.IsNullOrEmpty(types[0]))
                    {
                        errorMessage = "Algorithm type name not found, or unable to resolve multiple algorithm types to a single type. Please verify algorithm type name matches the algorithm name in the configuration file and that there is one and only one class derived from QCAlgorithm.";
                        Log.Error($"Loader.TryCreateILAlgorithm(): {errorMessage}");
                        return false;
                    }
                }
                //Load the assembly into this AppDomain:
                algorithmInstance = (IAlgorithm)assembly.CreateInstance(types[0], true);

                if (algorithmInstance != null)
                {
                    Log.Trace("Loader.TryCreateILAlgorithm(): Loaded " + algorithmInstance.GetType().Name);
                }

            }
            catch (ReflectionTypeLoadException err)
            {
                Log.Error(err);
                Log.Error("Loader.TryCreateILAlgorithm(1): " + err.LoaderExceptions[0]);
                if (err.InnerException != null) errorMessage = err.InnerException.Message;
            }
            catch (Exception err)
            {
                errorMessage = "Algorithm type name not found, or unable to resolve multiple algorithm types to a single type. Please verify algorithm type name matches the algorithm name in the configuration file and that there is one and only one class derived from QCAlgorithm.";
                errorMessage += err.InnerException == null ? err.Message : err.InnerException.Message;
                Log.Error($"Loader.TryCreateILAlgorithm(): {errorMessage}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get a list of all the matching type names in this DLL assembly:
        /// </summary>
        /// <param name="assembly">Assembly dll we're loading.</param>
        /// <returns>String list of types available.</returns>
        public static List<string> GetExtendedTypeNames(Assembly assembly)
        {
            var types = new List<string>();
            try
            {
                Type[] assemblyTypes;
                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    // We may want to exclude possible null values
                    // See https://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
                    assemblyTypes = e.Types.Where(t => t != null).ToArray();

                    var countTypesNotLoaded = e.LoaderExceptions.Length;
                    Log.Error($"Loader.GetExtendedTypeNames(): Unable to load {countTypesNotLoaded} of the requested types, " +
                              "see below for more details on what causes an issue:");

                    foreach (Exception inner in e.LoaderExceptions)
                    {
                        Log.Error($"Loader.GetExtendedTypeNames(): {inner.Message}");
                    }
                }

                if (assemblyTypes != null && assemblyTypes.Length > 0)
                {
                    types = (from t in assemblyTypes
                             where t.IsClass                                    // require class
                             where !t.IsAbstract                                // require concrete impl
                             where AlgorithmInterfaceType.IsAssignableFrom(t)   // require derived from IAlgorithm
                             where t.FullName != AlgorithmBaseTypeFullName      // require not equal to QuantConnect.QCAlgorithm
                             where t.FullName != FrameworkBaseTypeFullName      // require not equal to QuantConnect.QCAlgorithmFramework
                             where t.GetConstructor(Type.EmptyTypes) != null    // require default ctor
                             select t.FullName).ToList();
                }
                else
                {
                    Log.Error("API.GetExtendedTypeNames(): No types found in assembly.");
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return types;
        }

        /// <summary>
        /// Creates a new instance of the class in the library, safely.
        /// </summary>
        /// <param name="assemblyPath">Location of the DLL</param>
        /// <param name="ramLimit">Limit of the RAM for this process</param>
        /// <param name="algorithmInstance">Output algorithm instance</param>
        /// <param name="errorMessage">Output error message on failure</param>
        /// <returns>bool success</returns>
        public bool TryCreateAlgorithmInstanceWithIsolator(string assemblyPath, int ramLimit, out IAlgorithm algorithmInstance, out string errorMessage)
        {
            IAlgorithm instance = null;
            var error = string.Empty;

            var success = false;
            var isolator = new Isolator();
            var complete = isolator.ExecuteWithTimeLimit(_loaderTimeLimit, () =>
            {
                success = TryCreateAlgorithmInstance(assemblyPath, out instance, out error);
            }, ramLimit, sleepIntervalMillis:50, workerThread:_workerThread);

            algorithmInstance = instance;
            errorMessage = error;

            // if the isolator stopped us early add that to our error message
            if (!complete)
            {
                errorMessage = "Failed to create algorithm instance within 10 seconds. Try re-building algorithm. " + error;
            }

            return complete && success && algorithmInstance != null;
        }


        /// <summary>
        /// Unload this factory's appDomain.
        /// </summary>
        /// <remarks>Not used in lean engine. Running the library in an app domain is 10x slower.</remarks>
        /// <seealso cref="AppDomain.CreateDomain(string, Evidence, string, string, bool, AppDomainInitializer, string[])"/>
        public void Unload() {
            if (appDomain != null)
            {
                AppDomain.Unload(appDomain);
                appDomain = null;
            }
        }

    } // End Algorithm Factory Class

} // End QC Namespace.
