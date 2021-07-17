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
using System.Collections.Concurrent;

namespace QuantConnect.Util
{
    /// <summary>
    /// Dictionary change action
    /// </summary>
    public enum ChangeAction
    {
        /// <summary>
        /// No action
        /// </summary>
        None, 

        /// <summary>
        /// Item removed
        /// </summary>
        Delete,

        /// <summary>
        /// Item added
        /// </summary>
        Insert,

        /// <summary>
        /// Item updated (corresponds to dictionary's AddOrUpdate method)
        /// </summary>
        Update
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class NotifyingConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        /// <summary>
        /// The delegate that defines dictionary changed event's signature
        /// </summary>
        public delegate void DictionaryChangedEventHandler(object sender, DictChangedEventArgs<TKey, TValue> e);

        /// <summary>
        /// The dictionary changed event
        /// </summary>
        public event DictionaryChangedEventHandler OnDictionaryChanged;

        /// <summary>
        /// Uses the specified functions to add a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist, or to update a key/value pair if the key already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValue">The value to be added for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value</param>
        /// <returns></returns>
        public new TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            var value = base.AddOrUpdate(key, addValue, updateValueFactory);
            OnDictionaryChanged?.Invoke(this, new DictChangedEventArgs<TKey, TValue>(ChangeAction.Update, key, value));
            return value;
        }

        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// and raises <see cref="DictionaryChangedEventHandler"/> event to report changes
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <returns>true if the key/value pair was added successfully; false if the key already exists.</returns>
        public new bool TryAdd(TKey key, TValue value)
        {
            var result = base.TryAdd(key, value);
            OnDictionaryChanged?.Invoke(this, new DictChangedEventArgs<TKey, TValue>(ChangeAction.Insert, key, value));
            return result;
        }

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// and raises <see cref="DictionaryChangedEventHandler"/> event to report changes
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, contains the object removed from the the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// or the default value of the TValue type if key does not exist.</param>
        /// <returns>true if the object was removed successfully; otherwise, false.</returns>
        public new bool TryRemove(TKey key, out TValue value)
        {
            var result = base.TryRemove(key, out value);
            OnDictionaryChanged?.Invoke(this, new DictChangedEventArgs<TKey, TValue>(ChangeAction.Delete, key, value));
            return result;
        }
    }

    /// <summary>
    /// Event args for dictionary changed delegate in <see cref="NotifyingConcurrentDictionary{TKey,TValue}"/>
    /// </summary>
    /// <typeparam name="TKey">Key element type of the kvp being changed</typeparam>
    /// <typeparam name="TValue">Value element type of the kvp being changed</typeparam>
    public class DictChangedEventArgs<TKey, TValue> : EventArgs
    {
        /// <summary>
        /// Change event type
        /// </summary>
        public ChangeAction EventType { get; set; }

        /// <summary>
        /// Key element of the being changed kvp
        /// </summary>
        public TKey Key { get; set; }

        /// <summary>
        /// Value element of the being changed kvp
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="DictChangedEventArgs{TKey,TValue}"/>
        /// </summary>
        /// <param name="type">Change event type</param>
        /// <param name="key">Key element of the being changed kvp</param>
        /// <param name="value">Value element of the being changed kvp</param>
        public DictChangedEventArgs(ChangeAction type, TKey key, TValue value)
        {
            EventType = type;
            Key = key;
            Value = value;
        }
    }
}
