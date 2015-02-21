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
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for obtaining IBrokerageFactory instances
    /// </summary>
    public class Composer
    {
        /// <summary>
        /// Gets the singleton instance of BrokerageFactory
        /// </summary>
        public static Composer Instance = new Composer();

        // we really only need one of these per T for the whole application to use
        private Composer()
        {
            // grab assemblies from current executing directory
            var catalog = new DirectoryCatalog(Directory.GetCurrentDirectory());
            _compositionContainer = new CompositionContainer(catalog);
            _exportedValues = new Dictionary<Type, IEnumerable>();
        }

        private CompositionContainer _compositionContainer;
        private readonly Dictionary<Type, IEnumerable> _exportedValues;
        private readonly object _exportedValuesLockObject = new object();

        /// <summary>
        /// Gets the export matching the predicate
        /// </summary>
        /// <param name="predicate">Function used to pick which imported instance to return, if null the first instance is returned</param>
        /// <returns>A factory that produces the requested brokerage type, or null</returns>
        public T Single<T>(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            return GetExportedValues<T>().Single(predicate);
        }

        /// <summary>
        /// Resets the composition container to use the specified catalog
        /// </summary>
        /// <param name="catalog">The catalog containing parts to be exported</param>
        public void SetCatalog(ComposablePartCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }

            lock (_exportedValuesLockObject)
            {
                _exportedValues.Clear();
                _compositionContainer = new CompositionContainer(catalog);
            }
        }

        /// <summary>
        /// Resets the composition container
        /// </summary>
        /// <param name="compositionContainer">The composition container to search for exports</param>
        public void SetCompositionContainer(CompositionContainer compositionContainer)
        {
            if (compositionContainer == null)
            {
                throw new ArgumentNullException("compositionContainer");
            }

            lock (_exportedValuesLockObject)
            {
                _exportedValues.Clear();
                _compositionContainer = compositionContainer;
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
    }
}
