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

using Common.Util;
using QuantConnect.Python;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Provides a base class for types holding base data instances keyed by symbol
    /// </summary>
    [PandasNonExpandable]
    public class DataDictionary<T> : BaseExtendedDictionary<Symbol, T>
    {
        /// <summary>
        /// Used to cache the sorted items in the dictionary.
        /// We do this instead of using a SortedDictionary to keep the O(1) access time.
        /// </summary>
        private List<KeyValuePair<Symbol, T>> _items;
        private List<Symbol> _keys;
        private List<T> _values;

        /// <summary>
        /// Gets or sets the time associated with this collection of data
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class.
        /// </summary>
        public DataDictionary() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this data dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public DataDictionary(IEnumerable<T> data, Func<T, Symbol> keySelector)
            : base(data, keySelector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class.
        /// </summary>
        /// <param name="time">The time this data was emitted.</param>
        public DataDictionary(DateTime time) : base()
        {
            Time = time;
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        public override T this[Symbol symbol]
        {
            get
            {
                T data;
                if (TryGetValue(symbol, out data))
                {
                    return data;
                }
                CheckForImplicitlyCreatedSymbol(symbol);
                throw new KeyNotFoundException($"'{symbol}' wasn't found in the {GetType().GetBetterTypeName()} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{symbol}\")");
            }
            set
            {
                _items = null;
                base[symbol] = value;
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public virtual T GetValue(Symbol key)
        {
            T value;
            TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public override IEnumerable<KeyValuePair<Symbol, T>> GetItems()
        {
            if (_items == null)
            {
                _items = base.GetItems().OrderBy(x => x.Key).ToList();
            }
            return _items;
        }

        /// <summary>
        /// Gets a collection containing the keys of the dictionary
        /// </summary>
        public override ICollection<Symbol> Keys
        {
            get
            {
                if (_keys == null)
                {
                    _keys = (_items == null ? base.Keys.OrderBy(x => x) : _items.Select(x => x.Key)).ToList();
                }
                return _keys;
            }
        }

        /// <summary>
        /// Gets a collection containing the values of the dictionary
        /// </summary>
        public override ICollection<T> Values
        {
            get
            {
                if (_values == null)
                {
                    var items = _items == null
                        ? base.GetItems().OrderBy(x => x.Key)
                        : (IEnumerable<KeyValuePair<Symbol, T>>)_items;
                    _values = items.Select(x => x.Value).ToList();
                }
                return _values;
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary
        /// </summary>
        protected override IEnumerable<Symbol> GetKeys => Keys;

        /// <summary>
        /// Gets a collection containing the values in the dictionary
        /// </summary>
        protected override IEnumerable<T> GetValues => Values;

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary
        /// </summary>
        /// <returns>An enumerator for the dictionary</returns>
        public override IEnumerator<KeyValuePair<Symbol, T>> GetEnumerator()
        {
            return GetItems().GetEnumerator();
        }

        /// <summary>
        /// Removes all items from the dictionary
        /// </summary>
        public override void Clear()
        {
            ClearCache();
            base.Clear();
        }

        /// <summary>
        /// Removes the value with the specified key
        /// </summary>
        /// <param name="key">The key of the element to remove</param>
        /// <returns>true if the element was successfully found and removed; otherwise, false</returns>
        public override bool Remove(Symbol key)
        {
            ClearCache();
            return base.Remove(key);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to remove</param>
        /// <returns>true if the key-value pair was successfully removed; otherwise, false</returns>
        public override bool Remove(KeyValuePair<Symbol, T> item)
        {
            ClearCache();
            return base.Remove(item);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add</param>
        public override void Add(Symbol key, T value)
        {
            ClearCache();
            base.Add(key, value);
        }

        /// <summary>
        /// Adds an element with the provided key-value pair to the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to add</param>
        public override void Add(KeyValuePair<Symbol, T> item)
        {
            ClearCache();
            base.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearCache()
        {
            _items = null;
            _keys = null;
            _values = null;
        }
    }

    /// <summary>
    /// Provides extension methods for the DataDictionary class
    /// </summary>
    public static class DataDictionaryExtensions
    {
        /// <summary>
        /// Provides a convenience method for adding a base data instance to our data dictionary
        /// </summary>
        public static void Add<T>(this DataDictionary<T> dictionary, T data)
            where T : BaseData
        {
            dictionary.Add(data.Symbol, data);
        }
    }
}
