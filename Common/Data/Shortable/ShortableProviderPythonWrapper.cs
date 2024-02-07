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

using Python.Runtime;
using QuantConnect.Interfaces;
using QuantConnect.Python;
using System;

namespace QuantConnect.Data.Shortable
{
    /// <summary>
    /// Python wrapper for custom shortable providers
    /// </summary>
    public class ShortableProviderPythonWrapper : IShortableProvider
    {
        private readonly dynamic _shortableProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="shortableProvider">The python custom shortable provider</param>
        public ShortableProviderPythonWrapper(PyObject shortableProvider)
        {
            _shortableProvider = shortableProvider.ValidateImplementationOf<IShortableProvider>();
        }

        /// <summary>
        /// Gets the quantity shortable for a <see cref="Symbol"/>, from python custom shortable provider
        /// </summary>
        /// <param name="symbol">Symbol to check shortable quantity</param>
        /// <param name="localTime">Local time of the algorithm</param>
        /// <returns>The quantity shortable for the given Symbol as a positive number. Null if the Symbol is shortable without restrictions.</returns>
        public long? ShortableQuantity(Symbol symbol, DateTime localTime)
        {
            using (Py.GIL())
            {
                return (_shortableProvider.ShortableQuantity(symbol, localTime) as PyObject).GetAndDispose<long?>();
            }
        }
    }
}
