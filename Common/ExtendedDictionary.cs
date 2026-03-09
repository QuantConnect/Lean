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

using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Interfaces;
using System.Collections;
using QuantConnect.Python;

namespace QuantConnect
{
    /// <summary>
    /// Provides a base class for types holding key value pairs with helper methods for easy usage in Python
    /// </summary>
    [PandasNonExpandable]
#pragma warning disable CA1708 // Identifiers should differ by more than case
    public abstract class ExtendedDictionary<TKey, TValue> : IExtendedDictionary<TKey, TValue>
#pragma warning restore CA1708 // Identifiers should differ by more than case
    {
        /// <summary>
        /// Gets the number of elements contained in the dictionary
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public virtual void Clear()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(Messages.ExtendedDictionary.ClearInvalidOperation(this));
            }
            throw new NotImplementedException(Messages.ExtendedDictionary.ClearMethodNotImplemented);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public abstract bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Checks if the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary</param>
        /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
        public virtual bool ContainsKey(TKey key)
        {
            return TryGetValue(key, out _);
        }

        /// <summary>
        /// Gets all the items in the dictionary
        /// </summary>
        /// <returns>All the items in the dictionary</returns>
        public abstract IEnumerable<KeyValuePair<TKey, TValue>> GetItems();

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the key objects of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.ICollection`1"/> containing the key objects of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected abstract IEnumerable<TKey> GetKeys { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected abstract IEnumerable<TValue> GetValues { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IDictionary"/> object is read-only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public virtual bool IsReadOnly => true;

        /// <summary>
        /// Removes the value with the specified key
        /// </summary>
        /// <param name="key">The key object of the element to remove.</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
        public virtual bool Remove(TKey key)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(Messages.ExtendedDictionary.RemoveInvalidOperation(this));
            }
            throw new NotImplementedException(Messages.ExtendedDictionary.RemoveMethodNotImplemented);
        }

        /// <summary>
        /// Indexer method for the base dictioanry to access the objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="key">Key object indexer</param>
        /// <returns>Object of <typeparamref name="TValue"/></returns>
        public abstract TValue this[TKey key] { get; set; }

        /// <summary>
        /// Removes all keys and values from the <see cref="IExtendedDictionary{TKey, TValue}"/>.
        /// </summary>
        public void clear()
        {
            Clear();
        }

        /// <summary>
        /// Creates a shallow copy of the <see cref="IExtendedDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>Returns a shallow copy of the dictionary. It doesn't modify the original dictionary.</returns>
        public PyDict copy()
        {
            return fromkeys(GetKeys.ToArray());
        }

        /// <summary>
        /// Creates a new dictionary from the given sequence of elements.
        /// </summary>
        /// <param name="sequence">Sequence of elements which is to be used as keys for the new dictionary</param>
        /// <returns>Returns a new dictionary with the given sequence of elements as the keys of the dictionary.</returns>
        public PyDict fromkeys(TKey[] sequence)
        {
            return fromkeys(sequence, default);
        }

        /// <summary>
        /// Creates a new dictionary from the given sequence of elements with a value provided by the user.
        /// </summary>
        /// <param name="sequence">Sequence of elements which is to be used as keys for the new dictionary</param>
        /// <param name="value">Value which is set to each each element of the dictionary</param>
        /// <returns>Returns a new dictionary with the given sequence of elements as the keys of the dictionary.
        /// Each element of the newly created dictionary is set to the provided value.</returns>
        public PyDict fromkeys(TKey[] sequence, TValue value)
        {
            using (Py.GIL())
            {
                var dict = new PyDict();
                foreach (var key in sequence)
                {
                    var pyValue = get(key, value);
                    dict.SetItem(key.ToPython(), pyValue.ToPython());
                }
                return dict;
            }
        }

        /// <summary>
        /// Returns the value for the specified key if key is in dictionary.
        /// </summary>
        /// <param name="key">key to be searched in the dictionary</param>
        /// <returns>The value for the specified key if key is in dictionary.
        /// None if the key is not found and value is not specified.</returns>
        public TValue get(TKey key)
        {
            TValue data;
            TryGetValue(key, out data);
            return data;
        }

        /// <summary>
        /// Returns the value for the specified key if key is in dictionary.
        /// </summary>
        /// <param name="key">key to be searched in the dictionary</param>
        /// <param name="value">Value to be returned if the key is not found. The default value is null.</param>
        /// <returns>The value for the specified key if key is in dictionary.
        /// value if the key is not found and value is specified.</returns>
        public TValue get(TKey key, TValue value)
        {
            TValue data;
            if (TryGetValue(key, out data))
            {
                return data;
            }
            return value;
        }

        /// <summary>
        /// Returns a view object that displays a list of dictionary's (key, value) tuple pairs.
        /// </summary>
        /// <returns>Returns a view object that displays a list of a given dictionary's (key, value) tuple pair.</returns>
        public PyList items()
        {
            using (Py.GIL())
            {
                var pyList = new PyList();
                foreach (var (key, value) in GetItems())
                {
                    using var pyKey = key.ToPython();
                    using var pyValue = value.ToPython();
                    using var pyKvp = new PyTuple([pyKey, pyValue]);
                    pyList.Append(pyKvp);
                }
                return pyList;
            }
        }

        /// <summary>
        /// Returns and removes an arbitrary element (key, value) pair from the dictionary.
        /// </summary>
        /// <returns>Returns an arbitrary element (key, value) pair from the dictionary
        /// removes an arbitrary element(the same element which is returned) from the dictionary.
        /// Note: Arbitrary elements and random elements are not same.The popitem() doesn't return a random element.</returns>
        public PyTuple popitem()
        {
            throw new NotSupportedException(Messages.ExtendedDictionary.PopitemMethodNotSupported(this));
        }

        /// <summary>
        /// Returns the value of a key (if the key is in dictionary). If not, it inserts key with a value to the dictionary.
        /// </summary>
        /// <param name="key">Key with null/None value is inserted to the dictionary if key is not in the dictionary.</param>
        /// <returns>The value of the key if it is in the dictionary
        /// None if key is not in the dictionary</returns>
        public TValue setdefault(TKey key)
        {
            return setdefault(key, default);
        }

        /// <summary>
        /// Returns the value of a key (if the key is in dictionary). If not, it inserts key with a value to the dictionary.
        /// </summary>
        /// <param name="key">Key with a value default_value is inserted to the dictionary if key is not in the dictionary.</param>
        /// <param name="default_value">Default value</param>
        /// <returns>The value of the key if it is in the dictionary
        /// default_value if key is not in the dictionary and default_value is specified</returns>
        public TValue setdefault(TKey key, TValue default_value)
        {
            TValue data;
            if (TryGetValue(key, out data))
            {
                return data;
            }

            if (IsReadOnly)
            {
                throw new KeyNotFoundException(Messages.ExtendedDictionary.KeyNotFoundDueToNoData(this, key));
            }

            this[key] = default_value;
            return default_value;
        }

        /// <summary>
        /// Removes and returns an element from a dictionary having the given key.
        /// </summary>
        /// <param name="key">Key which is to be searched for removal</param>
        /// <returns>If key is found - removed/popped element from the dictionary
        /// If key is not found - KeyError exception is raised</returns>
        public TValue pop(TKey key)
        {
            return pop(key, default);
        }

        /// <summary>
        /// Removes and returns an element from a dictionary having the given key.
        /// </summary>
        /// <param name="key">Key which is to be searched for removal</param>
        /// <param name="default_value">Value which is to be returned when the key is not in the dictionary</param>
        /// <returns>If key is found - removed/popped element from the dictionary
        /// If key is not found - value specified as the second argument(default)</returns>
        public TValue pop(TKey key, TValue default_value)
        {
            TValue data;
            if (TryGetValue(key, out data))
            {
                Remove(key);
                return data;
            }
            return default_value;
        }

        /// <summary>
        /// Updates the dictionary with the elements from the another dictionary object or from an iterable of key/value pairs.
        /// The update() method adds element(s) to the dictionary if the key is not in the dictionary.If the key is in the dictionary, it updates the key with the new value.
        /// </summary>
        /// <param name="other">Takes either a dictionary or an iterable object of key/value pairs (generally tuples).</param>
        public void update(PyObject other)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(Messages.ExtendedDictionary.UpdateInvalidOperation(this));
            }

            var dictionary = other.ConvertToDictionary<TKey, TValue>();
            foreach (var kvp in dictionary)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Returns a view object that displays a list of all the key objects in the dictionary
        /// </summary>
        /// <returns>Returns a view object that displays a list of all the key objects.
        /// When the dictionary is changed, the view object also reflect these changes.</returns>
        public PyList keys()
        {
            return GetKeys.ToPyList();
        }

        /// <summary>
        /// Returns a view object that displays a list of all the values in the dictionary.
        /// </summary>
        /// <returns>Returns a view object that displays a list of all values in a given dictionary.</returns>
        public PyList values()
        {
            return GetValues.ToPyList();
        }

        /// <summary>
        /// Checks if the symbol is implicitly created from a string, in which case it is not in the symbol cache,
        /// and throws a KeyNotFoundException.
        /// </summary>
        protected void CheckForImplicitlyCreatedSymbol(Symbol symbol)
        {
            if (symbol.ID == new SecurityIdentifier(symbol.ID.Symbol, 0))
            {
                throw new KeyNotFoundException(Messages.ExtendedDictionary.TickerNotFoundInSymbolCache(symbol.ID.Symbol));
            }
        }
    }
}
