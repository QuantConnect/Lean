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

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Provides a base class for types holding base data instances keyed by symbol
    /// </summary>
    public class DataDictionary<T> : IDictionary<Symbol, T>
    {
        // storage for the data
        private readonly IDictionary<Symbol, T> _data = new Dictionary<Symbol, T>();

       /// <summary>
       /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class.
       /// </summary>
        public DataDictionary()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this data dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public DataDictionary(IEnumerable<T> data, Func<T, Symbol> keySelector)
        {
            foreach (var datum in data)
            {
                this[keySelector(datum)] = datum;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuantConnect.Data.Market.DataDictionary{T}"/> class.
        /// </summary>
        /// <param name="time">The time this data was emitted.</param>
        public DataDictionary(DateTime time)
        {
#pragma warning disable 618 // This assignment is left here until the Time property is removed.
            Time = time;
#pragma warning restore 618
        }

        /// <summary>
        /// Gets or sets the time associated with this collection of data
        /// </summary>
        [Obsolete("The DataDictionary<T> Time property is now obsolete. All algorithms should use algorithm.Time instead.")]
        public DateTime Time { get; set; }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<Symbol, T>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _data).GetEnumerator();
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public void Add(KeyValuePair<Symbol, T> item)
        {
            _data.Add(item);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
        /// </returns>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
        public bool Contains(KeyValuePair<Symbol, T> item)
        {
            return _data.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
        public void CopyTo(KeyValuePair<Symbol, T>[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
        public bool Remove(KeyValuePair<Symbol, T> item)
        {
            return _data.Remove(item);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </returns>
        public int Count
        {
            get { return _data.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
        /// </returns>
        public bool IsReadOnly
        {
            get { return _data.IsReadOnly; }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
        /// </returns>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool ContainsKey(Symbol key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param><param name="value">The object to use as the value of the element to add.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public void Add(Symbol key, T value)
        {
            _data.Add(key, value);
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <param name="key">The key of the element to remove.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public bool Remove(Symbol key)
        {
            return _data.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param><param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetValue(Symbol key, out T value)
        {
            return _data.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        /// <param name="symbol">The key of the element to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="symbol"/> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="symbol"/> is not found.</exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public T this[Symbol symbol]
        {
            get
            {
                T data;
                if (TryGetValue(symbol, out data))
                {
                    return data;
                }
                throw new KeyNotFoundException($"'{symbol}' wasn't found in the {GetType().GetBetterTypeName()} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{symbol}\")");
            }
            set
            {
                _data[symbol] = value;
            }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <returns>
        /// The element with the specified key.
        /// </returns>
        /// <param name="ticker">The key of the element to get or set.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="ticker"/> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="ticker"/> is not found.</exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public T this[string ticker]
        {
            get
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new KeyNotFoundException($"'{ticker}' wasn't found in the {GetType().GetBetterTypeName()} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{ticker}\")");
                }
                return this[symbol];
            }
            set
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new KeyNotFoundException($"'{ticker}' wasn't found in the {GetType().GetBetterTypeName()} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{ticker}\")");
                }
                this[symbol] = value;
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<Symbol> Keys
        {
            get { return _data.Keys; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<T> Values
        {
            get { return _data.Values; }
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <returns>
        /// The value associated with the specified key, if the key is found; otherwise, the default value for the type of the <typeparamref name="T"/> parameter.
        /// </returns>
        public virtual T GetValue(Symbol key)
        {
            T value;
            TryGetValue(key, out value);
            return value;
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