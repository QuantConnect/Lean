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
using QuantConnect;
using QuantConnect.Python;

namespace Common.Util
{
    /// <summary>
    /// Provides a generic implementation of ExtendedDictionary with specific dictionary type
    /// </summary>
    [PandasNonExpandable]
    public class BaseExtendedDictionary<TKey, TValue, TDictionary> : ExtendedDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TDictionary : IDictionary<TKey, TValue>, new()
    {
        /// <summary>
        /// The dictionary instance
        /// </summary>
        protected TDictionary Dictionary { get; }

        /// <summary>
        /// Initializes a new instance of the BaseExtendedDictionary class that is empty
        /// </summary>
        public BaseExtendedDictionary()
        {
            Dictionary = new TDictionary();
        }

        /// <summary>
        /// Initializes a new instance of the BaseExtendedDictionary class that contains elements copied from the specified dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are copied to the new dictionary</param>
        public BaseExtendedDictionary(TDictionary dictionary)
        {
            Dictionary = dictionary;
        }

        /// <summary>
        /// Initializes a new instance of the BaseExtendedDictionary class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public BaseExtendedDictionary(IEnumerable<TValue> data, Func<TValue, TKey> keySelector)
            : this()
        {
            foreach (var datum in data)
            {
                Dictionary[keySelector(datum)] = datum;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the dictionary
        /// </summary>
        public override int Count => Dictionary.Count;

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only
        /// </summary>
        public override bool IsReadOnly => Dictionary.IsReadOnly;

        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key whose value to get</param>
        /// <param name="value">When this method returns, the value associated with the specified key</param>
        /// <returns>true if the key was found; otherwise, false</returns>
        public override bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public override IEnumerable<KeyValuePair<TKey, TValue>> GetItems()
        {
            return Dictionary;
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary
        /// </summary>
        protected override IEnumerable<TKey> GetKeys => Dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values in the dictionary
        /// </summary>
        protected override IEnumerable<TValue> GetValues => Dictionary.Values;

        /// <summary>
        /// Gets a collection containing the keys of the dictionary
        /// </summary>
        public virtual ICollection<TKey> Keys => Dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values of the dictionary
        /// </summary>
        public virtual ICollection<TValue> Values => Dictionary.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get or set</param>
        /// <returns>The value associated with the specified key</returns>
        public override TValue this[TKey key]
        {
            get => Dictionary[key];
            set => Dictionary[key] = value;
        }

        /// <summary>
        /// Removes all items from the dictionary
        /// </summary>
        public override void Clear()
        {
            Dictionary.Clear();
        }

        /// <summary>
        /// Removes the value with the specified key
        /// </summary>
        /// <param name="key">The key of the element to remove</param>
        /// <returns>true if the element was successfully found and removed; otherwise, false</returns>
        public override bool Remove(TKey key)
        {
            return Dictionary.Remove(key);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the dictionary
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add</param>
        public virtual void Add(TKey key, TValue value)
        {
            Dictionary.Add(key, value);
        }

        /// <summary>
        /// Adds an element with the provided key-value pair to the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to add</param>
        public virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            Dictionary.Add(item);
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the dictionary</param>
        /// <returns>true if the dictionary contains an element with the specified key; otherwise, false</returns>
        public override bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific key-value pair
        /// </summary>
        /// <param name="item">The key-value pair to locate</param>
        /// <returns>true if the key-value pair was found; otherwise, false</returns>
        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the dictionary to an array, starting at a particular array index
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="arrayIndex">The starting index in the array</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Dictionary.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the dictionary
        /// </summary>
        /// <param name="item">The key-value pair to remove</param>
        /// <returns>true if the key-value pair was successfully removed; otherwise, false</returns>
        public virtual bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary
        /// </summary>
        /// <returns>An enumerator for the dictionary</returns>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
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

    /// <summary>
    /// Provides a default implementation of ExtendedDictionary using Dictionary{TKey, TValue}
    /// </summary>
    [PandasNonExpandable]
    public class BaseExtendedDictionary<TKey, TValue> : BaseExtendedDictionary<TKey, TValue, Dictionary<TKey, TValue>>
    {
        /// <summary>
        /// Initializes a new instance of the BaseExtendedDictionary class that is empty
        /// </summary>
        public BaseExtendedDictionary() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the BaseExtendedDictionary class that contains elements copied from the specified dictionary
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are copied to the new dictionary</param>
        public BaseExtendedDictionary(IDictionary<TKey, TValue> dictionary) : base(new Dictionary<TKey, TValue>(dictionary))
        {
        }

        /// <summary>
        /// Initializes a new instance of the BaseExtendedDictionary class
        /// using the specified <paramref name="data"/> as a data source
        /// </summary>
        /// <param name="data">The data source for this dictionary</param>
        /// <param name="keySelector">Delegate used to select a key from the value</param>
        public BaseExtendedDictionary(IEnumerable<TValue> data, Func<TValue, TKey> keySelector) : base(data, keySelector)
        {
        }
    }
}
