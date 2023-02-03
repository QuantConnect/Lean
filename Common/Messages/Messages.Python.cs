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

using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

using Python.Runtime;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Python"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing common messages for the <see cref="Python"/> namespace classes
        /// </summary>
        public static class PythonCommon
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string AttributeNotImplemented(string attribute, PyType pythonType)
            {
                return $"{attribute} must be implemented. Please implement this missing method on {pythonType}";
            }
        }

        /// <summary>
        /// Provides user-facing common messages for the <see cref="Python.MarginCallModelPythonWrapper"/> namespace classes
        /// </summary>
        public static class MarginCallModelPythonWrapper
        {
            public static string GetMarginCallOrdersMustReturnTuple = "Must return a tuple, where the first item is a list and the second a boolean";
        }

        /// <summary>
        /// Provides user-facing common messages for the <see cref="Python.PandasConverter"/> namespace classes
        /// </summary>
        public static class PandasConverter
        {
            public static string PandasModuleNotImported = "pandas module was not imported.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ConvertToDictionaryFailed(string sourceType, string targetType, string reason)
            {
                return $"ConvertToDictionary cannot be used to convert a {sourceType} into {targetType}. Reason: {reason}";
            }
        }

        /// <summary>
        /// Provides user-facing common messages for the <see cref="Python.PandasData"/> namespace classes
        /// </summary>
        public static class PandasData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string DuplicateKey(string duplicateKey, string type)
            {
                return $"More than one '{duplicateKey}' member was found in '{type}' class.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string KeyNotFoundInSeries(string key)
            {
                return $"{key} key does not exist in series dictionary.";
            }
        }

        /// <summary>
        /// Provides user-facing common messages for the <see cref="Python.PythonInitializer"/> namespace classes
        /// </summary>
        public static class PythonInitializer
        {
            public static string Start = "start";

            public static string Ended = "ended";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToLocateAlgorithm(string algorithmLocation)
            {
                return $"Unable to find algorithm location path: {algorithmLocation}.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string VirutalEnvironmentNotFound(string virtualEnvPath)
            {
                return $"Path {virtualEnvPath} to virtual environment does not exist.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string FailedToFindSystemPackagesConfiguration(string virtualEnvPath, FileInfo configFile)
            {
                return $@"virtual env '{virtualEnvPath}'. Failed to find system packages configuration. ConfigFile.Exits: {
                    configFile.Exists}. Will default to true.";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SystemPackagesConfigurationFound(string virtualEnvPath, bool includeSystemPackages)
            {
                return $"virtual env '{virtualEnvPath}'. Will use system packages: {includeSystemPackages}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string PythonPathNotFound(string pythonPath)
            {
                return $"Unable to find python path: {pythonPath}. Skipping.";
            }
        }

        /// <summary>
        /// Provides user-facing common messages for the <see cref="Python.PythonWrapper"/> namespace classes
        /// </summary>
        public static class PythonWrapper
        {
            public static string ExpectedInterfaceTypeParameter = "expected an interface type parameter.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InterfaceNotFullyImplemented(string interfaceName, string pythonTypeName, IEnumerable<string> missingMembers)
            {
                return $@"{interfaceName} must be fully implemented. Please implement these missing methods on {
                    pythonTypeName}: {string.Join(", ", missingMembers)}";
            }
        }
    }
}
