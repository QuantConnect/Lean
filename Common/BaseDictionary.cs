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

namespace QuantConnect
{
    /// <summary>
    /// Provides a base class for types holding instances keyed by symbol
    /// </summary>
    public abstract class BaseDictionary<T> : IExtendedDictionary<Symbol, T>
    {
        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
        public virtual void Clear()
        {
            throw new InvalidOperationException("");
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <param name="key">The key whose value to get.</param><param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param><exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public abstract bool TryGetValue(Symbol key, out T value);

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected abstract IEnumerable<Symbol> GetKeys { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        protected abstract IEnumerable<T> GetValues { get; }

        public virtual bool Remove(Symbol symbol)
        {
            throw new InvalidOperationException("");
        }

        public virtual T this[Symbol symbol]
        {
            get
            {
                throw new InvalidOperationException("");
            }
            set
            {
                throw new InvalidOperationException("");
            }
        }

        public virtual T this[string symbol]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void clear()
        {
            Clear();
        }

        public PyDict copy()
        {
            return fromkeys(GetKeys.ToArray());
        }

        public PyDict fromkeys(Symbol[] sequence)
        {
            return fromkeys(sequence, default(T));
        }

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

        public T get(Symbol symbol)
        {
            T data;
            if (TryGetValue(symbol, out data))
            {
                return data;
            }
            throw new KeyNotFoundException($"'{symbol}' wasn't found in the Slice object, likely because there was no-data at this moment in time and it wasn't possible to fillforward historical data. Please check the data exists before accessing it with data.ContainsKey(\"{symbol}\")");
        }

        public T get(Symbol symbol, T value)
        {
            T data;
            if (TryGetValue(symbol, out data))
            {
                return data;
            }
            return value;
        }

        public IEnumerable<PyTuple> items()
        {
            foreach (var key in GetKeys)
            {
                object data = this[key];
                using (Py.GIL())
                {
                    yield return new PyTuple(new PyObject[] { key.ToPython(), data.ToPython() });
                }
            }
        }

        public PyTuple popitem()
        {
            throw new Exception("popitem method is not supported");
        }

        public T setdefault(Symbol symbol)
        {
            return setdefault(symbol, default(T));
        }

        public T setdefault(Symbol symbol, T default_value)
        {
            T data;
            if (TryGetValue(symbol, out data))
            {
                return data;
            }
            throw new Exception($"Dictionary is read-only: cannot set default value to {default_value} for {symbol}");
        }

        public T pop(Symbol symbol)
        {
            return pop(symbol, default(T));
        }

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

        public void update(PyDict other)
        {
            throw new Exception("popitem method is not supported");
        }

        public IEnumerable<Symbol> keys()
        {
            return GetKeys;
        }

        public IEnumerable<T> values()
        {
            return GetValues.ToList();
        }
    }
}