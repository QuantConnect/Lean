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

using QuantConnect.Data;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Provides an enumerator for sorting collections of <see cref="BaseData"/> objects based on a specified property.
    /// The sorting occurs lazily, only when enumeration begins.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for sorting.</typeparam>
    public class SortEnumerator<TKey> : IEnumerator<BaseData>
    {
        private readonly IEnumerable<BaseData> _data;
        private IEnumerator<BaseData> _sortedEnumerator;
        private readonly bool _preSorted;
        private readonly Func<BaseData, TKey> _keySelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortEnumerator{TKey}"/> class.
        /// </summary>
        /// <param name="data">The collection of <see cref="BaseData"/> to enumerate over.</param>
        /// <param name="keySelector">A function that defines the key to sort by.</param>
        /// <param name="preSorted">
        /// A flag indicating whether the data is already sorted by the selected key.
        /// <c>true</c> means the data is NOT sorted and will be sorted by this enumerator.
        /// <c>false</c> means the data is already sorted and will be returned as is.
        /// </param>
        public SortEnumerator(IEnumerable<BaseData> data, Func<BaseData, TKey> keySelector, bool preSorted = false)
        {
            _data = data;
            _keySelector = keySelector;
            _preSorted = preSorted;
        }

        /// <summary>
        /// Lazily retrieves the sorted or unsorted data based on the <see cref="_preSorted"/> flag.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="BaseData"/>.</returns>
        private IEnumerable<BaseData> GetSortedData()
        {
            if (_preSorted)
            {
                foreach (var item in _data.OrderBy(_keySelector))
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in _data)
                {
                    yield return item;
                }
            }
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Gets the current <see cref="BaseData"/> element in the collection.
        /// </summary>
        public BaseData Current
        {
            get
            {
                if (_sortedEnumerator == null)
                    throw new InvalidOperationException("Enumerator is not initialized.");
                return _sortedEnumerator.Current;
            }
        }


        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next element; 
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            if (_sortedEnumerator == null)
            {
                // Create the sorted enumerator lazily when MoveNext is first called
                _sortedEnumerator = GetSortedData().GetEnumerator();
            }
            return _sortedEnumerator.MoveNext();
        }

        /// <summary>
        /// Resets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _sortedEnumerator = null;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="SortEnumerator{TKey}"/>.
        /// </summary>
        public void Dispose()
        {
            _sortedEnumerator?.Dispose();
        }
    }
}
