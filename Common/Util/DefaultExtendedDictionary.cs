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
using System.Linq;
using QuantConnect;
using QuantConnect.Python;

namespace Common.Util
{
    /// <summary>
    /// Provides a default implementation of ExtendedDictionary that can be used with any key-value pair types
    /// </summary>
    [PandasNonExpandable]
    public class DefaultExtendedDictionary<TKey, TValue> : ExtendedDictionary<TKey, TValue>, IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// Initializes a new instance of the DefaultExtendedDictionary class that is empty
        /// </summary>
        public DefaultExtendedDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new instance of the DefaultExtendedDictionary class that contains elements copied from the specified dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are copied to the new dictionary</param>
        public DefaultExtendedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the DefaultExtendedDictionary class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public DefaultExtendedDictionary(IEnumerable<TValue> data, Func<TValue, TKey> keySelector)
        {
            _dictionary = new Dictionary<TKey, TValue>();
            foreach (var datum in data)
            {
                _dictionary[keySelector(datum)] = datum;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the dictionary
        /// </summary>
        public override int Count => _dictionary.Count;

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only
        /// </summary>
        public override bool IsReadOnly => false;

        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key whose value to get</param>
        /// <param name="value">When this method returns, the value associated with the specified key</param>
        /// <returns>true if the key was found; otherwise, false</returns>
        public override bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public override IEnumerable<KeyValuePair<TKey, TValue>> GetItems()
        {
            return _dictionary;
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary
        /// </summary>
        protected override IEnumerable<TKey> GetKeys => _dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values in the dictionary
        /// </summary>
        protected override IEnumerable<TValue> GetValues => _dictionary.Values;

        /// <summary>
        /// Gets a collection containing the keys of the dictionary
        /// </summary>
        public ICollection<TKey> Keys => _dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values of the dictionary
        /// </summary>
        public ICollection<TValue> Values => _dictionary.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get or set</param>
        /// <returns>The value associated with the specified key</returns>
        public override TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        /// <summary>
        /// Removes all items from the dictionary
        /// </summary>
        public override void Clear()
        {
            _dictionary.Clear();
        }

        /// <summary>
        /// Removes the value with the specified key
        /// </summary>
        /// <param name="key">The key of the element to remove</param>
        /// <returns>true if the element was successfully found and removed; otherwise, false</returns>
        public override bool Remove(TKey key)
        {
            return _dictionary.Remove(key);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add</param>
        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
        }

        /// <summary>
        /// Adds an element with the provided key-value pair to the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to add</param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the dictionary</param>
        /// <returns>true if the dictionary contains an element with the specified key; otherwise, false</returns>
        public override bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific key-value pair
        /// </summary>
        /// <param name="item">The key-value pair to locate</param>
        /// <returns>true if the key-value pair was found; otherwise, false</returns>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the dictionary to an array, starting at a particular array index
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="arrayIndex">The starting index in the array</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to remove</param>
        /// <returns>true if the key-value pair was successfully removed; otherwise, false</returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary
        /// </summary>
        /// <returns>An enumerator for the dictionary</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary
        /// </summary>
        /// <returns>An enumerator for the dictionary</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}