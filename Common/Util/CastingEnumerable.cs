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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Defines a list that casts the elements of a source list to a derived type.
    /// This is useful to avoid materializing another list after using, for example, the <see cref="Enumerable.Cast{TResult}(IEnumerable)"/> LINQ method.
    /// </summary>
    /// <typeparam name="TBase">The base type of the elements in the source enumerable.</typeparam>
    /// <typeparam name="TDerived">The type to cast the elements to.</typeparam>
    public class CastingEnumerable<TBase, TDerived> : IReadOnlyList<TDerived>
        where TDerived : class, TBase
    {
        private IReadOnlyList<TBase> _data;

        /// <summary>
        /// Gets the count of items in the enumerable.
        /// </summary>
        public int Count => _data.Count;

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index.</returns>
        public TDerived this[int index] => (TDerived)_data[index];

        /// <summary>
        /// Initializes a new instance of the <see cref="CastingEnumerable{TBase, TDerived}"/> class
        /// </summary>
        public CastingEnumerable(IReadOnlyList<TBase> data)
        {
            _data = data;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<TDerived> GetEnumerator()
        {
            foreach (var item in _data)
            {
                yield return (TDerived)item;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An enumerator object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
