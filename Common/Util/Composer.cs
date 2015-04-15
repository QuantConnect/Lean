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
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for obtaining exported MEF instances
    /// </summary>
    public class Composer
    {
        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static readonly Composer Instance = new Composer();

        private Composer()
        {
            // grab assemblies from current executing directory
            var catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);
            _compositionContainer = new CompositionContainer(catalog);
            _exportedValues = new Dictionary<Type, IEnumerable>();
        }

        private readonly CompositionContainer _compositionContainer;
        private readonly Dictionary<Type, IEnumerable> _exportedValues;
        private readonly object _exportedValuesLockObject = new object();

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
            var values = Instance.GetExportedValues<T>().ToList();

            // search the values by type to find the requested type
            var matchingType = values.Select(x => x.GetType()).FirstOrDefault(type => type.MatchesTypeName(typeName));
            if (matchingType == null)
            {
                throw new ArgumentException("Unable to locate any exports matching the requested typeName: " + typeName, "typeName");
            }

            return values.First(x => x.GetType() == matchingType);
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
    }
}
