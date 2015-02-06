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
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides methods for obtaining IBrokerageFactory instances
    /// </summary>
    public class Composer<T>
    {
        /// <summary>
        /// Gets the singleton instance of BrokerageFactory
        /// </summary>
        public static Composer<T> Instance = new Composer<T>();

        // we really only need one of these per T for the whole application to use
        private Composer() { }

        private bool _hasComposed;
        private IEnumerable<T> _composedInstances = new List<T>();

        /// <summary>
        /// Gets the first factory that produces the requested brokerage type
        /// </summary>
        /// <param name="predicate">Function used to pick which imported instance to return, if null the first instance is returned</param>
        /// <returns>A factory that produces the requested brokerage type, or null</returns>
        public T GetInstance(Func<T, bool> predicate)
        {
            // if we never composed, compose using all loaded types
            if (!_hasComposed) Compose();

            predicate = predicate ?? (x => true);
            return _composedInstances.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Enumerates over all the composed instances
        /// </summary>
        /// <returns>An enumerable of all composed instances</returns>
        public IEnumerable<T> GetInstances()
        {
            // if we never composed, compose using all loaded types
            if (!_hasComposed) Compose();

            // we purposefully select into identity to create a new enumerable to prevent callers from modifying its contents
            return _composedInstances.Select(x => x);
        }

        /// <summary>
        /// Composes the available IBrokerageFactory instances from the specified composition container. If the container is null,
        /// then one wil be created that searches all loaded types.
        /// </summary>
        /// <param name="container">The composition container to use to search for brokerage factories, or null to search all loaded types</param>
        public void Compose(CompositionContainer container = null)
        {
            if (container == null)
            {
                // create a catalog containing all loaded assemblies in the current app domain
                var catalog = new AggregateCatalog(AppDomain.CurrentDomain.GetAssemblies().Select(x => new AssemblyCatalog(x)));

                // define a container for all loaded assemblies
                container = new CompositionContainer(catalog);
            }

            // save off the instances composed from the container
            _composedInstances = container.GetExportedValues<T>();

            _hasComposed = true;
        }
    }
}
