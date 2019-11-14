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
using System.ComponentModel.Composition.ReflectionModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for obtaining exported MEF instances
    /// </summary>
    public class Composer
    {
        // this is purposefully a computed property to ensure setting 'plugin-directory' config will actually
        // be used. the only requirement is that the value is set before accessing Composer.Instance
        private static string PluginDirectory => Config.Get("plugin-directory");

        // grab assemblies from current executing directory if not defined by 'composer-dll-directory' configuration key
        private static readonly string PrimaryDllLookupDirectory = new DirectoryInfo(
            Config.Get("composer-dll-directory", AppDomain.CurrentDomain.BaseDirectory)
        ).FullName;

        private static readonly bool LoadFromPluginDirectory = !string.IsNullOrWhiteSpace(PluginDirectory)
            && new DirectoryInfo(PluginDirectory).FullName != PrimaryDllLookupDirectory
            && Directory.Exists(PluginDirectory);

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        /// <remarks>Intentionally using a property so that when its gotten it will
        /// trigger the lazy construction which will be after the right configuration
        /// is loaded. See GH issue 3258</remarks>
        public static readonly Composer Instance = new Composer();

        private Task<CompositionContainer> _compositionContainer;
        private readonly object _exportedValuesLockObject = new object();

        // dictionaries keyed by contract type
        private readonly Dictionary<Type, IEnumerable> _exportedValues = new Dictionary<Type, IEnumerable>();
        private readonly ConcurrentDictionary<Type, List<QCExportType>> _qcExportedTypes = new ConcurrentDictionary<Type, List<QCExportType>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Composer"/> class. This type
        /// is a light wrapper on top of an MEF <see cref="CompositionContainer"/>
        /// </summary>
        private Composer()
        {
            Initialize();
        }

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
                if (_exportedValues.TryGetValue(typeof (T), out values))
                {
                    ((IList<T>) values).Add(instance);
                }
                else
                {
                    values = new List<T> {instance};
                    _exportedValues[typeof (T)] = values;
                }
            }
        }

        /// <summary>
        /// Extension method to searches the composition container for an export that has a matching type name. This function
        /// will first try to match on Type.AssemblyQualifiedName, then Type.FullName, and finally on Type.Name
        ///
        /// This method will not throw if multiple types are found matching the name, it will just return the first one it finds.
        /// </summary>
        /// <typeparam name="T">The type of the export</typeparam>
        /// <param name="typeName">The name of the type to find. This can be an assembly qualified name, a full name, or just the type's name</param>
        /// <returns>The export instance</returns>
        public T GetExportedValueByTypeName<T>(string typeName)
            where T : class
        {
            try
            {
                lock (_exportedValuesLockObject)
                {
                    T instance = null;
                    IEnumerable values;
                    var type = typeof(T);
                    if (_exportedValues.TryGetValue(type, out values))
                    {
                        // if we've already loaded this part, then just return the same one
                        instance = values.OfType<T>().FirstOrDefault(x => x.GetType().MatchesTypeName(typeName));
                        if (instance != null)
                        {
                            return instance;
                        }
                    }

                    List<QCExportType> qcExports;
                    if (_qcExportedTypes.TryGetValue(type, out qcExports))
                    {
                        var qcExport = qcExports.FirstOrDefault(qce => qce.Implementation.MatchesTypeName(typeName));
                        if (qcExport != null)
                        {
                            instance = qcExport.GetInstance<T>();
                        }
                    }

                    if (instance == null)
                    {
                        // we want to get the requested part without instantiating each one of that type
                        var selectedPart = _compositionContainer.GetAwaiter().GetResult().Catalog.Parts
                            .Select(x => new { part = x, Type = ReflectionModelServices.GetPartType(x).Value })
                            .Where(x => type.IsAssignableFrom(x.Type))
                            .Where(x => x.Type.MatchesTypeName(typeName))
                            .Select(x => x.part)
                            .FirstOrDefault();

                        if (selectedPart == null)
                        {
                            throw new ArgumentException(
                                $"Unable to locate any exports matching the requested typeName: {typeName}", nameof(typeName));
                        }

                        var exportDefinition =
                            selectedPart.ExportDefinitions.First(
                                x => x.ContractName == AttributedModelServices.GetContractName(type));
                        instance = (T) selectedPart.CreatePart().GetExportedValue(exportDefinition);
                    }

                    var exportedParts = instance.GetType().GetInterfaces()
                        .Where(interfaceType => interfaceType.GetCustomAttribute<InheritedExportAttribute>() != null);

                    foreach (var export in exportedParts)
                    {
                        var exportList = _exportedValues.SingleOrDefault(kvp => kvp.Key == export).Value;

                        // cache the new value for next time
                        if (exportList == null)
                        {
                            var list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(export));
                            list.Add(instance);
                            _exportedValues[export] = list;
                        }
                        else
                        {
                            ((IList) exportList).Add(instance);
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

                if (err.InnerException != null)
                {
                    Log.Error(err.InnerException);
                }

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

                    List<QCExportType> qcExports;
                    if (_qcExportedTypes.TryGetValue(typeof(T), out qcExports))
                    {
                        return qcExports.Select(qce => qce.GetInstance<T>());
                    }

                    values = _compositionContainer.GetAwaiter().GetResult().GetExportedValues<T>().ToList();
                    _exportedValues[typeof (T)] = values;
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
            lock(_exportedValuesLockObject)
            {
                _exportedValues.Clear();

                // This is excluded to maintain faster tests, but I'm wondering why we can't just
                // avoid calling Reset() in our tests if we want to reuse the same instances and if
                // that's not an option, then perhaps adding a different method which supports keeping
                // some types to make it explicit, since the Composer impl now doesn't provide any
                // mechanism for actually resetting the state and starting fresh with new instances

                // See: e4fc00efbf876e39280344187017d707c325ca56 and  9865fe63e0da90c85ba26ecd4fb0067b7bbab825

                //_qcExportedTypes.Clear();
                //Initialize();
            }
        }

        /// <summary>
        /// Performs a hard reset of the composer. This includes completely re-initializing all of the state,
        /// including resolution of assemblies from disk.
        /// </summary>
        public void HardReset()
        {
            lock (_exportedValuesLockObject)
            {
                _exportedValues.Clear();
                _qcExportedTypes.Clear();
                Initialize();
            }
        }

        private void Initialize()
        {
            // for performance we will load QC assemblies and keep their exported types which is much faster
            // than using CompositionContainer which tries to handle many more cases than simply invoking the
            // public default parameterless constructor, such as dependency injection of properties, etc
            var files = EnumerateFiles(PrimaryDllLookupDirectory, $"{nameof(QuantConnect)}.*.dll")
                .Concat(EnumerateFiles(PrimaryDllLookupDirectory, $"{nameof(QuantConnect)}.*.exe"));

            if (LoadFromPluginDirectory)
            {
                var pluginsDirectory = new DirectoryInfo(PluginDirectory);
                var executingDirectory = new DirectoryInfo(PrimaryDllLookupDirectory);
                if (!string.Equals(pluginsDirectory.FullName, executingDirectory.FullName))
                {
                    // skip the plugin directory if it's the same as the primary. there's no chance for potential duplicates here since
                    // we're ensuring they're different directories and above we've ensured that we're loading different file extensions
                    files = files.Concat(EnumerateFiles(PluginDirectory, $"{nameof(QuantConnect)}.*.dll"));
                }
            }

            // load non-qc types in a task, most times we don't even need to evaluate this container, so we'll let
            // it initialize in the background and if we actually do need to look in it, it will likely be ready by
            // then or we'll wait for this task to complete
            _compositionContainer = Task.Run(() =>
            {
                // since we've already loaded assemblies matching the pattern: QuantConnect.*{dll|exe}
                // then we don't need to load them again, so look for assemblies not starting with QuantConnect
                var assemblyCatalogs = EnumerateFiles(PrimaryDllLookupDirectory, "*.dll")
                    .Concat(EnumerateFiles(PrimaryDllLookupDirectory, "*.exe"))
                    .Union(LoadFromPluginDirectory ? EnumerateFiles(PluginDirectory, "*.dll") : Enumerable.Empty<string>())
                    .AsParallel() // parallelize assembly loading
                    .Select(TryLoadAssembly)
                    .Where(assembly => assembly != null)
                    .Select(assembly => new AssemblyCatalog(assembly));

                // re-seed the composition container, making sure to exclude QuantConnect.*.{dll|exe} since we manually loaded those above
                return new CompositionContainer(new AggregateCatalog(assemblyCatalogs));
            });

            Parallel.ForEach(files, file =>
            {
                try
                {
                    var assembly = TryLoadAssembly(file);
                    if (assembly == null)
                    {
                        return;
                    }

                    // enumerate types in this assembly that have the [InheritedExport] on themselves or their interfaces
                    // QCExportType.ForImplementation will generate an export for each contract that has [InheritedExport]
                    var types = assembly.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        var export = QCExportType.ForImplementation(types[i]);
                        if (export == null)
                        {
                            continue;
                        }

                        // be sure to support requesting types directly by type name
                        _qcExportedTypes[export.Implementation] = new List<QCExportType> {export};

                        for (var j = 0; j < export.Contracts.Count; j++)
                        {
                            var contract = export.Contracts[j];

                            // support requesting types by the contract
                            lock (contract)
                            {
                                _qcExportedTypes.AddOrUpdate(contract,
                                    c => new List<QCExportType> { export },
                                    (c, list) => { list.Add(export); return list; }
                                );
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    Log.Error($"Composer.Initialize(): {file}", error);
                }
            });
        }

        private static IEnumerable<string> EnumerateFiles(string directory, string pattern)
        {
            return Directory.EnumerateFiles(directory, pattern);
        }

        private static Assembly TryLoadAssembly(string path)
        {
            try
            {
                return Assembly.LoadFrom(path);
            }
            catch (Exception error)
            {
                Log.Error($"Composer.TryLoadAssembly(): {path}", error);
                return null;
            }
        }

        private sealed class QCExportType
        {
            private object _instance;
            private readonly object _sync = new object();

            public readonly Type Implementation;
            public readonly Func<object> Activator;
            public readonly IReadOnlyList<Type> Contracts;

            public QCExportType(IEnumerable<Type> contracts, Type implementation, Func<object> activator)
            {
                Activator = activator;
                Contracts = contracts.ToList();
                Implementation = implementation;
            }

            public T GetInstance<T>()
            {
                if (_instance != null)
                {
                    return (T) _instance;
                }

                lock (_sync)
                {
                    _instance = (T) Activator();
                    return (T) _instance;
                }
            }

            public static QCExportType ForImplementation(Type type)
            {
                // require 'newable' via public/default constructor since we use the Activator w/ no args
                if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    return null;
                }

                if (type.Name.Contains("Export") || type.Name.Contains("MapFileProvider"))
                {

                }

                var contracts = new List<Type>();

                // inherit only captures base classes, we still need to check interfaces manually
                foreach (var inter in type.GetInterfaces())
                {
                    var attr = inter.GetCustomAttribute<InheritedExportAttribute>();
                    if (attr != null)
                    {
                        contracts.Add(inter);
                    }
                }

                if (contracts.Count == 0)
                {
                    var attr = type.GetCustomAttribute<InheritedExportAttribute>(inherit: true);
                    if (attr == null)
                    {
                        // we didn't find [InheritedExport] on any interfaces or the implementation type, bail.
                        return null;
                    }
                }

                // everyone exports their own contract, but only if we've found an [InheritedExport]
                contracts.Add(type);
                return new QCExportType(contracts, type, () => System.Activator.CreateInstance(type));
            }
        }
    }
}