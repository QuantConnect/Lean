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

using System;
using System.Linq;
using QuantConnect.Data;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Provides an enumerator for sorting collections of <see cref="BaseData"/> objects based on a specified property.
    /// The sorting occurs lazily, only when enumeration begins.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used for sorting.</typeparam>
    public class SortEnumerator<TKey> : IEnumerator<BaseData>, IDisposable
    {
        private bool isDisposed;
        private readonly IEnumerable<BaseData> _data;
        private IEnumerator<BaseData> _sortedEnumerator;
        private readonly Func<BaseData, TKey> _keySelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortEnumerator{TKey}"/> class.
        /// </summary>
        /// <param name="data">The collection of <see cref="BaseData"/> to enumerate over.</param>
        /// <param name="keySelector">A function that defines the key to sort by. Defaults to sorting by <see cref="BaseData.EndTime"/>.</param>
        public SortEnumerator(IEnumerable<BaseData> data, Func<BaseData, TKey> keySelector = null)
        {
            _data = data;
            _sortedEnumerator = GetSortedData().GetEnumerator();
            _keySelector = keySelector ??= baseData => (TKey)(object)baseData.EndTime;
        }

        /// <summary>
        /// Static method to wrap an enumerable with the sort enumerator.
        /// </summary>
        /// <param name="preSorted">Indicates if the data is pre-sorted.</param>
        /// <param name="data">The data to be wrapped into the enumerator.</param>
        /// <returns>An enumerator over the <see cref="BaseData"/>.</returns>
        public static IEnumerator<BaseData> TryWrapSortEnumerator(bool preSorted, IEnumerable<BaseData> data)
        {
            return preSorted ? new SortEnumerator<TKey>(data) : data.GetEnumerator();
        }

        /// <summary>
        /// Lazily retrieves the sorted data.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="BaseData"/>.</returns>
        private IEnumerable<BaseData> GetSortedData()
        {
            foreach (var item in _data.OrderBy(_keySelector))
            {
                yield return item;
            }
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Gets the current <see cref="BaseData"/> element in the collection.
        /// </summary>
        public BaseData Current
        {
            get => _sortedEnumerator.Current;
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
        /// Releases all resources used by the <see cref="SortEnumerator{TKey}"/> and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged and, optionally, managed resources used by the <see cref="SortEnumerator{TKey}"/>.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            if (disposing)
            {
                _sortedEnumerator?.Dispose();
            }
            isDisposed = true;
        }
    }
}
