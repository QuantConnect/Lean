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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.IO;
using System.Linq;
using System.Reflection;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for obtaining exported MEF instances
    /// </summary>
    public class Composer
    {
        private static readonly string PluginDirectory = Config.Get("plugin-directory");

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static readonly Composer Instance = new Composer();

        /// <summary>
        /// Initializes a new instance of the <see cref="Composer"/> class. This type
        /// is a light wrapper on top of an MEF <see cref="CompositionContainer"/>
        /// </summary>
        public Composer()
        {
            Reset();
        }

        private CompositionContainer _compositionContainer;
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
                throw new ArgumentNullException("predicate");
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
                    T instance;
                    IEnumerable values;
                    var type = typeof(T);
                    if (_exportedValues.TryGetValue(type, out values))
                    {
                        // if we've alread loaded this part, then just return the same one
                        instance = values.OfType<T>().FirstOrDefault(x => x.GetType().MatchesTypeName(typeName));
                        if (instance != null)
                        {
                            return instance;
                        }
                    }

                    // we want to get the requested part without instantiating each one of that type
                    var selectedPart = _compositionContainer.Catalog.Parts
                        .Select(x => new { part = x, Type = ReflectionModelServices.GetPartType(x).Value })
                        .Where(x => type.IsAssignableFrom(x.Type))
                        .Where(x => x.Type.MatchesTypeName(typeName))
                        .Select(x => x.part)
                        .FirstOrDefault();

                    if (selectedPart == null)
                    {
                        throw new ArgumentException(
                            "Unable to locate any exports matching the requested typeName: " + typeName, "typeName");
                    }

                    var exportDefinition =
                        selectedPart.ExportDefinitions.First(
                            x => x.ContractName == AttributedModelServices.GetContractName(type));
                    instance = (T)selectedPart.CreatePart().GetExportedValue(exportDefinition);

                    // cache the new value for next time
                    if (values == null)
                    {
                        values = new List<T> { instance };
                        _exportedValues[type] = values;
                    }
                    else
                    {
                        ((List<T>)values).Add(instance);
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
            lock (_exportedValuesLockObject)
            {
                IEnumerable values;
                if (_exportedValues.TryGetValue(typeof (T), out values))
                {
                    return values.OfType<T>();
                }

                values = _compositionContainer.GetExportedValues<T>().ToList();
                _exportedValues[typeof (T)] = values;
                return values.OfType<T>();
            }
        }

        /// <summary>
        /// Clears the cache of exported values, causing new instances to be created.
        /// </summary>
        public void Reset()
        {
            lock(_exportedValuesLockObject)
            {
                // grab assemblies from current executing directory
                var catalogs = new List<ComposablePartCatalog>
                {
                    new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory, "*.dll"),
                    new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory, "*.exe")
                };
                if (!string.IsNullOrWhiteSpace(PluginDirectory) && Directory.Exists(PluginDirectory) && new DirectoryInfo(PluginDirectory).FullName != AppDomain.CurrentDomain.BaseDirectory)
                {
                    catalogs.Add(new DirectoryCatalog(PluginDirectory, "*.dll"));
                }
                var aggregate = new AggregateCatalog(catalogs);
                _compositionContainer = new CompositionContainer(aggregate);
                _exportedValues.Clear();
            }
        }
    }
}
