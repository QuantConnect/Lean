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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for obtaining exported MEF instances
    /// </summary>
    public class Composer
    {
        private static string PluginDirectory;
        private static readonly Lazy<Composer> LazyComposer = new Lazy<Composer>(
            () =>
            {
                PluginDirectory = Config.Get("plugin-directory");
                return new Composer();
            });

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        /// <remarks>Intentionally using a property so that when its gotten it will
        /// trigger the lazy construction which will be after the right configuration
        /// is loaded. See GH issue 3258</remarks>
        public static Composer Instance => LazyComposer.Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Composer"/> class. This type
        /// is a light wrapper on top of an MEF <see cref="CompositionContainer"/>
        /// </summary>
        public Composer()
        {
            // Determine what directory to grab our assemblies from if not defined by 'composer-dll-directory' configuration key
            var dllDirectoryString = Config.Get("composer-dll-directory");
            if (string.IsNullOrWhiteSpace(dllDirectoryString))
            {
                // Check our appdomain directory for QC Dll's, for most cases this will be true and fine to use
                if (!string.IsNullOrEmpty(AppDomain.CurrentDomain.BaseDirectory) && Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "QuantConnect.*.dll").Any())
                {
                    dllDirectoryString = AppDomain.CurrentDomain.BaseDirectory;
                }
                else
                {
                    // Otherwise check out our parent and current working directory
                    // this is helpful for research because kernel appdomain defaults to kernel location
                    var currentDirectory = Directory.GetCurrentDirectory();
                    var parentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory; // If parent == null will just use current

                    // If our parent directory contains QC Dlls use it, otherwise default to current working directory
                    // In cloud and CLI research cases we expect the parent directory to contain the Dlls; but locally it's likely current directory
                    dllDirectoryString = Directory.GetFiles(parentDirectory, "QuantConnect.*.dll").Any() ? parentDirectory : currentDirectory;
                }
            }

            // Resolve full path name just to be safe
            var primaryDllLookupDirectory = new DirectoryInfo(dllDirectoryString).FullName;
            Log.Trace($"Composer(): Loading Assemblies from {primaryDllLookupDirectory}");

            var loadFromPluginDir = !string.IsNullOrWhiteSpace(PluginDirectory)
                && Directory.Exists(PluginDirectory) &&
                new DirectoryInfo(PluginDirectory).FullName != primaryDllLookupDirectory;
            _composableParts = Task.Run(() =>
            {
                try
                {
                    var catalogs = new List<ComposablePartCatalog>
                    {
                        new DirectoryCatalog(primaryDllLookupDirectory, "*.dll"),
                        new DirectoryCatalog(primaryDllLookupDirectory, "*.exe")
                    };
                    if (loadFromPluginDir)
                    {
                        catalogs.Add(new DirectoryCatalog(PluginDirectory, "*.dll"));
                    }
                    var aggregate = new AggregateCatalog(catalogs);
                    _compositionContainer = new CompositionContainer(aggregate);
                    return _compositionContainer.Catalog.Parts.ToList();
                }
                catch (Exception exception)
                {
                    // ThreadAbortException is triggered when we shutdown ignore the error log
                    if (!(exception is ThreadAbortException))
                    {
                        Log.Error(exception);
                    }
                }
                return new List<ComposablePartDefinition>();
            });

            // for performance we will load our assemblies and keep their exported types
            // which is much faster that using CompositionContainer which uses reflexion
            var exportedTypes = new ConcurrentBag<Type>();
            var fileNames = Directory.EnumerateFiles(primaryDllLookupDirectory, $"{nameof(QuantConnect)}.*.dll");
            if (loadFromPluginDir)
            {
                fileNames = fileNames.Concat(Directory.EnumerateFiles(PluginDirectory, $"{nameof(QuantConnect)}.*.dll"));
            }

            // guarantee file name uniqueness
            var files = new Dictionary<string, string>();
            foreach (var filePath in fileNames)
            {
                var fileName = Path.GetFileName(filePath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    files[fileName] = filePath;
                }
            }
            Parallel.ForEach(files.Values,
                file =>
                {
                    try
                    {
                        foreach (var type in
                            Assembly.LoadFrom(file).ExportedTypes.Where(type => !type.IsAbstract && !type.IsInterface && !type.IsEnum))
                        {
                            exportedTypes.Add(type);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored, just in case
                    }
                }
            );
            _exportedTypes = new List<Type>(exportedTypes);
        }

        private CompositionContainer _compositionContainer;
        private readonly IReadOnlyList<Type> _exportedTypes;
        private readonly Task<List<ComposablePartDefinition>> _composableParts;
        private readonly object _exportedValuesLockObject = new object();
        private readonly Dictionary<Type, IEnumerable> _exportedValues = new Dictionary<Type, IEnumerable>();

        /// <summary>
        /// Gets the export matching the predicate
        /// </summary>
        /// <param name="predicate">Function used to pick which imported instance to return, if null the first instance is returned</param>
        /// <returns>The only export matching the specified predicate</returns>
        public T Single<T>(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return GetExportedValues<T>().Single(predicate);
        }

        /// <summary>
        /// Adds the specified instance to this instance to allow it to be recalled via GetExportedValueByTypeName
        /// </summary>
        /// <typeparam name="T">The contract type</typeparam>
        /// <param name="instance">The instance to add</param>
        public void AddPart<T>(T instance)
        {
            lock (_exportedValuesLockObject)
            {
                IEnumerable values;
                if (_exportedValues.TryGetValue(typeof(T), out values))
                {
                    ((IList<T>)values).Add(instance);
                }
                else
                {
                    values = new List<T> { instance };
                    _exportedValues[typeof(T)] = values;
                }
            }
        }

        /// <summary>
        /// Gets the first type T instance if any
        /// </summary>
        /// <typeparam name="T">The contract type</typeparam>
        public T GetPart<T>()
        {
            return GetPart<T>(null);
        }

        /// <summary>
        /// Gets the first type T instance if any
        /// </summary>
        /// <typeparam name="T">The contract type</typeparam>
        public T GetPart<T>(Func<T, bool> filter)
        {
            lock (_exportedValuesLockObject)
            {
                IEnumerable values;
                if (_exportedValues.TryGetValue(typeof(T), out values))
                {
                    return ((IList<T>)values).Where(x => filter == null || filter(x)).FirstOrDefault();
                }
                return default(T);
            }
        }

        /// <summary>
        /// Will return all loaded types that are assignable to T type
        /// </summary>
        public IEnumerable<Type> GetExportedTypes<T>() where T : class
        {
            var type = typeof(T);
            return _exportedTypes.Where(type1 =>
                {
                    try
                    {
                        return type.IsAssignableFrom(type1);
                    }
                    catch
                    {
                        return false;
                    }
                });
        }

        /// <summary>
        /// Extension method to searches the composition container for an export that has a matching type name. This function
        /// will first try to match on Type.AssemblyQualifiedName, then Type.FullName, and finally on Type.Name
        ///
        /// This method will not throw if multiple types are found matching the name, it will just return the first one it finds.
        /// </summary>
        /// <typeparam name="T">The type of the export</typeparam>
        /// <param name="typeName">The name of the type to find. This can be an assembly qualified name, a full name, or just the type's name</param>
        /// <param name="forceTypeNameOnExisting">When false, if any existing instance of type T is found, it will be returned even if type name doesn't match.
        /// This is useful in cases where a single global instance is desired, like for <see cref="IDataAggregator"/></param>
        /// <returns>The export instance</returns>
        public T GetExportedValueByTypeName<T>(string typeName, bool forceTypeNameOnExisting = true)
            where T : class
        {
            try
            {
                T instance = null;
                IEnumerable values;
                var type = typeof(T);
                lock (_exportedValuesLockObject)
                {
                    if (_exportedValues.TryGetValue(type, out values))
                    {
                        // if we've already loaded this part, then just return the same one
                        instance = values.OfType<T>().FirstOrDefault(x => !forceTypeNameOnExisting || x.GetType().MatchesTypeName(typeName));
                        if (instance != null)
                        {
                            return instance;
                        }
                    }
                }

                var typeT = _exportedTypes.Where(type1 =>
                    {
                        try
                        {
                            return type.IsAssignableFrom(type1) && type1.MatchesTypeName(typeName);
                        }
                        catch
                        {
                            return false;
                        }
                    })
                .FirstOrDefault();

                if (typeT != null)
                {
                    instance = (T)Activator.CreateInstance(typeT);
                }

                if (instance == null)
                {
                    // we want to get the requested part without instantiating each one of that type
                    var selectedPart = _composableParts.Result
                        .Where(x =>
                            {
                                try
                                {
                                    var xType = ReflectionModelServices.GetPartType(x).Value;
                                    return type.IsAssignableFrom(xType) && xType.MatchesTypeName(typeName);
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                        )
                        .FirstOrDefault();

                    if (selectedPart == null)
                    {
                        throw new ArgumentException(
                            $"Unable to locate any exports matching the requested typeName: {typeName}. Type: {type}", nameof(typeName));
                    }

                    var exportDefinition =
                        selectedPart.ExportDefinitions.First(
                            x => x.ContractName == AttributedModelServices.GetContractName(type));
                    instance = (T)selectedPart.CreatePart().GetExportedValue(exportDefinition);
                }

                var exportedParts = instance.GetType().GetInterfaces()
                    .Where(interfaceType => interfaceType.GetCustomAttribute<InheritedExportAttribute>() != null);

                lock (_exportedValuesLockObject)
                {
                    foreach (var export in exportedParts)
                    {
                        var exportList = _exportedValues.SingleOrDefault(kvp => kvp.Key == export).Value;

                        // cache the new value for next time
                        if (exportList == null)
                        {
                            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(export));
                            list.Add(instance);
                            _exportedValues[export] = list;
                        }
                        else
                        {
                            ((IList)exportList).Add(instance);
                        }
                    }

                    return instance;
                }
            }
            catch (ReflectionTypeLoadException err)
            {
                foreach (var exception in err.LoaderExceptions)
                {
                    Log.Error(exception);
                    Log.Error(exception.ToString());
                }

                if (err.InnerException != null) Log.Error(err.InnerException);

                throw;
            }
        }
        /// <summary>
        /// Gets all exports of type T
        /// </summary>
        public IEnumerable<T> GetExportedValues<T>()
        {
            try
            {
                lock (_exportedValuesLockObject)
                {
                    IEnumerable values;
                    if (_exportedValues.TryGetValue(typeof(T), out values))
                    {
                        return values.OfType<T>();
                    }

                    if (!_composableParts.IsCompleted)
                    {
                        _composableParts.Wait();
                    }
                    values = _compositionContainer.GetExportedValues<T>().ToList();
                    _exportedValues[typeof(T)] = values;
                    return values.OfType<T>();
                }
            }
            catch (ReflectionTypeLoadException err)
            {
                foreach (var exception in err.LoaderExceptions)
                {
                    Log.Error(exception);
                }

                throw;
            }
        }

        /// <summary>
        /// Clears the cache of exported values, causing new instances to be created.
        /// </summary>
        public void Reset()
        {
            lock (_exportedValuesLockObject)
            {
                _exportedValues.Clear();
            }
        }
    }
}
