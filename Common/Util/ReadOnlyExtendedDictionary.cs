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
using QuantConnect.Python;

namespace Common.Util
{
    /// <summary>
    /// Provides a read-only implementation of ExtendedDictionary
    /// </summary>
    [PandasNonExpandable]
    public class ReadOnlyExtendedDictionary<TKey, TValue> : BaseExtendedDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the ReadOnlyExtendedDictionary class that is empty
        /// </summary>
        public ReadOnlyExtendedDictionary()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReadOnlyExtendedDictionary class that contains elements copied from the specified dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are copied to the new dictionary</param>
        public ReadOnlyExtendedDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ReadOnlyExtendedDictionary class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public ReadOnlyExtendedDictionary(IEnumerable<TValue> data, Func<TValue, TKey> keySelector) : base(data, keySelector)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only
        /// </summary>
        public override bool IsReadOnly => true;

        /// <summary>
        /// Gets an enumerable collection containing the keys of the read-only dictionary
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => base.Keys;

        /// <summary>
        /// Gets an enumerable collection containing the values of the read-only dictionary
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => base.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get or set</param>
        /// <returns>The value associated with the specified key</returns>
        public override TValue this[TKey key]
        {
            get => base[key];
            set => throw new InvalidOperationException("Dictionary is read-only");
        }

        /// <summary>
        /// Removes all items from the dictionary
        /// </summary>
        public override void Clear()
        {
            throw new InvalidOperationException("Dictionary is read-only");
        }

        /// <summary>
        /// Removes the value with the specified key
        /// </summary>
        /// <param name="key">The key of the element to remove</param>
        /// <returns>true if the element was successfully found and removed; otherwise, false</returns>
        public override bool Remove(TKey key)
        {
            throw new InvalidOperationException("Dictionary is read-only");
        }

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add</param>
        public new void Add(TKey key, TValue value)
        {
            throw new InvalidOperationException("Dictionary is read-only");
        }

        /// <summary>
        /// Adds an element with the provided key-value pair to the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to add</param>
        public new void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new InvalidOperationException("Dictionary is read-only");
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to remove</param>
        /// <returns>true if the key-value pair was successfully removed; otherwise, false</returns>
        public new bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new InvalidOperationException("Dictionary is read-only");
        }
    }
}