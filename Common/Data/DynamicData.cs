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
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Python.Runtime;
using QuantConnect.Util;

namespace QuantConnect.Data
{
    /// <summary>
    /// Dynamic Data Class: Accept flexible data, adapting to the columns provided by source.
    /// </summary>
    /// <remarks>Intended for use with Quandl class.</remarks>
    public abstract class DynamicData : BaseData, IDynamicMetaObjectProvider
    {
        private static readonly MethodInfo SetPropertyMethodInfo = typeof(DynamicData).GetMethod("SetProperty");
        private static readonly MethodInfo GetPropertyMethodInfo = typeof(DynamicData).GetMethod("GetProperty");

        private readonly IDictionary<string, object> _storage = new Dictionary<string, object>();

        /// <summary>
        /// Get the metaObject required for Dynamism.
        /// </summary>
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new GetSetPropertyDynamicMetaObject(parameter, this, SetPropertyMethodInfo, GetPropertyMethodInfo);
        }

        /// <summary>
        /// Sets the property with the specified name to the value. This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to set</param>
        /// <param name="value">The new property value</param>
        /// <returns>Returns the input value back to the caller</returns>
        public object SetProperty(string name, object value)
        {
            name = name.LazyToLower();

            if (name == "time")
            {
                if (value is PyObject pyobject)
                {
                    Time = pyobject.As<DateTime>();
                }
                else
                {
                    Time = (DateTime)value;
                }
            }
            else if (name == "endtime" || name == "end_time")
            {
                if (value is PyObject pyobject)
                {
                    EndTime = pyobject.As<DateTime>();
                }
                else
                {
                    EndTime = (DateTime)value;
                }
            }
            else if (name == "value")
            {
                if (value is PyObject pyobject)
                {
                    Value = pyobject.As<decimal>();
                }
                else
                {
                    Value = (decimal)value;
                }
            }
            else if (name == "symbol")
            {
                if (value is string)
                {
                    Symbol = SymbolCache.GetSymbol((string) value);
                }
                else
                {
                    if (value is PyObject pyobject)
                    {
                        Symbol = pyobject.As<Symbol>();
                    }
                    else
                    {
                        Symbol = (Symbol)value;
                    }
                }
            }

            _storage[name] = value;
            return value;
        }

        /// <summary>
        /// Gets the property's value with the specified name. This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to access</param>
        /// <returns>object value of BaseData</returns>
        public object GetProperty(string name)
        {
            name = name.ToLowerInvariant();

            // redirect these calls to the base types properties
            if (name == "time")
            {
                return Time;
            }
            if (name == "endtime")
            {
                return EndTime;
            }
            if (name == "value")
            {
                return Value;
            }
            if (name == "symbol")
            {
                return Symbol;
            }
            if (name == "price")
            {
                return Price;
            }

            object value;
            if (!_storage.TryGetValue(name, out value))
            {
                // let the user know the property name that we couldn't find
                throw new KeyNotFoundException(
                    $"Property with name \'{name}\' does not exist. Properties: Time, Symbol, Value {string.Join(", ", _storage.Keys)}"
                );
            }

            return value;
        }

        /// <summary>
        /// Gets whether or not this dynamic data instance has a property with the specified name.
        /// This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to check for</param>
        /// <returns>True if the property exists, false otherwise</returns>
        public bool HasProperty(string name)
        {
            return _storage.ContainsKey(name.ToLowerInvariant());
        }

        /// <summary>
        /// Gets the storage dictionary
        /// Python algorithms need this information since DynamicMetaObject does not work
        /// </summary>
        /// <returns>Dictionary that stores the paramenters names and values</returns>
        public IDictionary<string, object> GetStorageDictionary()
        {
            return _storage;
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            var clone = ObjectActivator.Clone(this);
            foreach (var kvp in _storage)
            {
                // don't forget to add the dynamic members!
                clone._storage.Add(kvp);
            }
            return clone;
        }
    }
}
