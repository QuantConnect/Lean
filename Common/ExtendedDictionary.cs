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

namespace QuantConnect
{
    /// <summary>
    /// Provides a base class for types holding instances keyed by <see cref="Symbol"/>
    /// </summary>
    public abstract class ExtendedDictionary<T> : IExtendedDictionary<Symbol, T>
    {
        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public virtual void Clear()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException($"Clear/clear method call is an invalid operation. {GetType().Name} is a read-only collection.");
            }
            throw new NotImplementedException("Types deriving from 'ExtendedDictionary' must implement the 'void Clear() method.");
        }

        /// <summary>
        /// Gets the value associated with the specified Symbol.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified Symbol; otherwise, false.
        /// </returns>
        /// <param name="symbol">The Symbol whose value to get.</param><param name="value">When this method returns, the value associated with the specified Symbol, if the Symbol is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param><exception cref="T:System.ArgumentNullException"><paramref name="symbol"/> is null.</exception>
        public abstract bool TryGetValue(Symbol symbol, out T value);

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the Symbol objects of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the Symbol objects of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected abstract IEnumerable<Symbol> GetKeys { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected abstract IEnumerable<T> GetValues { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IDictionary"/> object is read-only.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        public virtual bool IsReadOnly => true;

        /// <summary>
        /// Removes the value with the specified Symbol
        /// </summary>
        /// <param name="symbol">The Symbol object of the element to remove.</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
        public virtual bool Remove(Symbol symbol)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException($"Remove/pop method call is an invalid operation. {GetType().Name} is a read-only collection.");
            }
            throw new NotImplementedException("Types deriving from 'ExtendedDictionary' must implement the 'void Remove(Symbol) method.");
        }

        /// <summary>
        /// Indexer method for the base dictioanry to access the objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="symbol">Symbol object indexer</param>
        /// <returns>Object of <typeparamref name="T"/></returns>
        public virtual T this[Symbol symbol]
        {
            get
            {
                throw new NotImplementedException("Types deriving from 'ExtendedDictionary' must implement the 'T this[Symbol] method.");
            }
            set
            {
                throw new NotImplementedException("Types deriving from 'ExtendedDictionary' must implement the 'T this[Symbol] method.");
            }
        }

        /// <summary>
        /// Indexer method for the base dictioanry to access the objects by their symbol.
        /// </summary>
        /// <remarks>IDictionary implementation</remarks>
        /// <param name="ticker">string ticker symbol indexer</param>
        /// <returns>Object of <typeparamref name="T"/></returns>
        public virtual T this[string ticker]
        {
            get
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new KeyNotFoundException($"The ticker {ticker} was not found in the SymbolCache. Use the Symbol object as key instead. Accessing the securities collection/slice object by string ticker is only available for securities added with the AddSecurity-family methods. For more details, please check out the documentation.");
                }
                return this[symbol];
            }
            set
            {
                Symbol symbol;
                if (!SymbolCache.TryGetSymbol(ticker, out symbol))
                {
                    throw new KeyNotFoundException($"The ticker {ticker} was not found in the SymbolCache. Use the Symbol object as key instead. Accessing the securities collection/slice object by string ticker is only available for securities added with the AddSecurity-family methods. For more details, please check out the documentation.");
                }
                this[symbol] = value;
            }
        }

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
        public PyDict fromkeys(Symbol[] sequence)
        {
            return fromkeys(sequence, default(T));
        }

        /// <summary>
        /// Creates a new dictionary from the given sequence of elements with a value provided by the user.
        /// </summary>
        /// <param name="sequence">Sequence of elements which is to be used as keys for the new dictionary</param>
        /// <param name="value">Value which is set to each each element of the dictionary</param>
        /// <returns>Returns a new dictionary with the given sequence of elements as the keys of the dictionary.
        /// Each element of the newly created dictionary is set to the provided value.</returns>
        public PyDict fromkeys(Symbol[] sequence, T value)
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
        /// Returns the value for the specified Symbol if Symbol is in dictionary.
        /// </summary>
        /// <param name="symbol">Symbol to be searched in the dictionary</param>
        /// <returns>The value for the specified Symbol if Symbol is in dictionary.
        /// None if the Symbol is not found and value is not specified.</returns>
        public T get(Symbol symbol)
        {
            T data;
            TryGetValue(symbol, out data);
            return data;
        }

        /// <summary>
        /// Returns the value for the specified Symbol if Symbol is in dictionary.
        /// </summary>
        /// <param name="symbol">Symbol to be searched in the dictionary</param>
        /// <param name="value">Value to be returned if the Symbol is not found. The default value is null.</param>
        /// <returns>The value for the specified Symbol if Symbol is in dictionary.
        /// value if the Symbol is not found and value is specified.</returns>
        public T get(Symbol symbol, T value)
        {
            T data;
            if (TryGetValue(symbol, out data))
            {
                return data;
            }
            return value;
        }

        /// <summary>
        /// Returns a view object that displays a list of dictionary's (Symbol, value) tuple pairs.
        /// </summary>
        /// <returns>Returns a view object that displays a list of a given dictionary's (Symbol, value) tuple pair.</returns>
        public PyList items()
        {
            using (Py.GIL())
            {
                var pyList = new PyList();
                foreach (var key in GetKeys)
                {
                    using (var pyKey = key.ToPython())
                    {
                        using (var pyValue = this[key].ToPython())
                        {
                            using (var pyObject = new PyTuple(new PyObject[] { pyKey, pyValue }))
                            {
                                pyList.Append(pyObject);
                            }
                        }
                    }
                }
                return pyList;
            }
        }

        /// <summary>
        /// Returns and removes an arbitrary element (Symbol, value) pair from the dictionary.
        /// </summary>
        /// <returns>Returns an arbitrary element (Symbol, value) pair from the dictionary
        /// removes an arbitrary element(the same element which is returned) from the dictionary.
        /// Note: Arbitrary elements and random elements are not same.The popitem() doesn't return a random element.</returns>
        public PyTuple popitem()
        {
            throw new NotSupportedException($"popitem method is not supported for {GetType().Name}");
        }

        /// <summary>
        /// Returns the value of a Symbol (if the Symbol is in dictionary). If not, it inserts Symbol with a value to the dictionary.
        /// </summary>
        /// <param name="symbol">Key with null/None value is inserted to the dictionary if Symbol is not in the dictionary.</param>
        /// <returns>The value of the Symbol if it is in the dictionary
        /// None if Symbol is not in the dictionary</returns>
        public T setdefault(Symbol symbol)
        {
            return setdefault(symbol, default(T));
        }

        /// <summary>
        /// Returns the value of a Symbol (if the Symbol is in dictionary). If not, it inserts Symbol with a value to the dictionary.
        /// </summary>
        /// <param name="symbol">Key with a value default_value is inserted to the dictionary if Symbol is not in the dictionary.</param>
        /// <param name="default_value">Default value</param>
        /// <returns>The value of the Symbol if it is in the dictionary
        /// default_value if Symbol is not in the dictionary and default_value is specified</returns>
        public T setdefault(Symbol symbol, T default_value)
        {
            T data;
            if (TryGetValue(symbol, out data))
            {
                return data;
            }

            if (IsReadOnly)
            {
                throw new KeyNotFoundException($"'{symbol}' wasn't found in the {GetType().Name} object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{symbol}\"). The collection is read-only, cannot set default.");
            }

            this[symbol] = default_value;
            return default_value;
        }

        /// <summary>
        /// Removes and returns an element from a dictionary having the given Symbol.
        /// </summary>
        /// <param name="symbol">Key which is to be searched for removal</param>
        /// <returns>If Symbol is found - removed/popped element from the dictionary
        /// If Symbol is not found - KeyError exception is raised</returns>
        public T pop(Symbol symbol)
        {
            return pop(symbol, default(T));
        }

        /// <summary>
        /// Removes and returns an element from a dictionary having the given Symbol.
        /// </summary>
        /// <param name="symbol">Key which is to be searched for removal</param>
        /// <param name="default_value">Value which is to be returned when the Symbol is not in the dictionary</param>
        /// <returns>If Symbol is found - removed/popped element from the dictionary
        /// If Symbol is not found - value specified as the second argument(default)</returns>
        public T pop(Symbol symbol, T default_value)
        {
            T data;
            if (TryGetValue(symbol, out data))
            {
                Remove(symbol);
                return data;
            }
            return default_value;
        }

        /// <summary>
        /// Updates the dictionary with the elements from the another dictionary object or from an iterable of Symbol/value pairs.
        /// The update() method adds element(s) to the dictionary if the Symbol is not in the dictionary.If the Symbol is in the dictionary, it updates the Symbol with the new value.
        /// </summary>
        /// <param name="other">Takes either a dictionary or an iterable object of Symbol/value pairs (generally tuples).</param>
        public void update(PyDict other)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException($"update method call is an invalid operation. {GetType().Name} is a read-only collection.");
            }

            var dictionary = other.ConvertToDictionary<Symbol, T>();
            foreach (var kvp in dictionary)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Returns a view object that displays a list of all the Symbol objects in the dictionary
        /// </summary>
        /// <returns>Returns a view object that displays a list of all the Symbol objects.
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
    }
}