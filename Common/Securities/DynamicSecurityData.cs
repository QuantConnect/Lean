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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Python.Runtime;
using QuantConnect.Data;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides access to a security's data via it's type. This implementation supports dynamic access
    /// by type name.
    /// </summary>
    public class DynamicSecurityData : IDynamicMetaObjectProvider
    {
        private static readonly MethodInfo SetPropertyMethodInfo = typeof(DynamicSecurityData).GetMethod("SetProperty");
        private static readonly MethodInfo GetPropertyMethodInfo = typeof(DynamicSecurityData).GetMethod("GetProperty");

        private readonly IRegisteredSecurityDataTypesProvider _registeredTypes;
        private readonly ConcurrentDictionary<string, Lazy<object>> _storage = new ConcurrentDictionary<string, Lazy<object>>();
        private readonly ConcurrentDictionary<Type, Type> _genericTypes = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicSecurityData"/> class
        /// </summary>
        /// <param name="registeredTypes">Provides all the registered data types for the algorithm</param>
        public DynamicSecurityData(IRegisteredSecurityDataTypesProvider registeredTypes)
        {
            _registeredTypes = registeredTypes;
        }

        /// <summary>Returns the <see cref="T:System.Dynamic.DynamicMetaObject" /> responsible for binding operations performed on this object.</summary>
        /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> to bind this object.</returns>
        /// <param name="parameter">The expression tree representation of the runtime value.</param>
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new GetSetPropertyDynamicMetaObject(parameter, this, SetPropertyMethodInfo, GetPropertyMethodInfo);
        }

        /// <summary>
        /// Gets whether or not this dynamic data instance has data stored for the specified type
        /// </summary>
        public bool HasData<T>()
        {
            return _storage.ContainsKey(typeof(T).Name);
        }

        /// <summary>
        /// Gets whether or not this dynamic data instance has a property with the specified name.
        /// This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to check for</param>
        /// <returns>True if the property exists, false otherwise</returns>
        public bool HasProperty(string name)
        {
            return _storage.ContainsKey(name);
        }

        /// <summary>
        /// Stores the list of data using <paramref name="dataType"/>'s <see cref="Type.Name"/> as the key
        /// </summary>
        /// <typeparam name="T">Statically known type of each item in the list. This may just be BaseData
        /// which is why this method supports explicitly providing the data type to use for the key</typeparam>
        /// <param name="dataType">The runtime type of each item in the list. The name of this type is used
        /// as the key for the data</param>
        /// <param name="data">The data to be stored</param>
        public void StoreData<T>(Type dataType, IReadOnlyList<T> data)
        {
            StoreData(typeof(T), dataType, (IList)data);
        }

        /// <summary>
        /// Gets the last item in the data list for the specified type
        /// </summary>
        public T Get<T>()
        {
            var list = GetAll<T>();
            return list.LastOrDefault();
        }

        /// <summary>
        /// Gets the data list for the specified type
        /// </summary>
        public IReadOnlyList<T> GetAll<T>()
        {
            return GetAllImpl(typeof(T));
        }

        /// <summary>
        /// Get the matching cached object in a python friendly accessor
        /// </summary>
        /// <param name="type">Type to search for</param>
        /// <returns>Matching object</returns>
        public PyObject Get(Type type)
        {
            var list = GetAll(type);

            if (list.Count == 0)
            {
                return null;
            }

            using (Py.GIL())
            {
                return list[list.Count - 1].ToPython();
            }
        }

        /// <summary>
        /// Get all the matching types with a python friendly overload.
        /// </summary>
        /// <param name="type">Search type</param>
        /// <returns>List of matching objects cached</returns>
        public IList GetAll(Type type)
        {
            return GetAllImpl(type);
        }

        /// <summary>
        /// Sets the property with the specified name to the value. This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to set</param>
        /// <param name="value">The new property value</param>
        /// <returns>Returns the input value back to the caller</returns>
        public object SetProperty(string name, Lazy<object> value)
        {
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
            Lazy<object> value;
            if (_storage.TryGetValue(name, out value))
            {
                return value.Value;
            }

            // check to see if the requested name matches one of the algorithm registered data types and if
            // so, we'll return a new empty list. this precludes us from always needing to check HasData<T>
            Type type;
            if (_registeredTypes.TryGetType(name, out type))
            {
                var listType = GetGenericListType(type);
                return Activator.CreateInstance(listType);
            }

            var keys = _storage.Keys.OrderBy(k => k);
            throw new KeyNotFoundException($"Property with name '{name}' does not exist. Properties: {string.Join(", ", keys)}");

        }

        /// <summary>
        /// Get all implementation that covers both Python and C#
        /// </summary>
        private dynamic GetAllImpl(Type type)
        {
            var data = GetProperty(type.Name);

            var dataType = data.GetType();
            if (dataType.GetElementType() == type // covers arrays
                // covers lists
                || dataType.GenericTypeArguments.Length == 1
                && dataType.GenericTypeArguments[0] == type)
            {
                return data;
            }

            var baseDataList = data as IReadOnlyList<BaseData>;
            if (baseDataList != null)
            {
                var listType = GetGenericListType(type);
                var list = (IList)Activator.CreateInstance(listType);
                foreach (var baseData in baseDataList)
                {
                    list.Add(baseData);
                }
                StoreData(type, type, list);
                return list;
            }

            throw new InvalidOperationException(
                $"Expected a list with type '{type.GetBetterTypeName()}' " +
                $"but found type '{data.GetType().GetBetterTypeName()}"
            );
        }

        /// <summary>
        /// Store data implementation that covers both Python and C#
        /// </summary>
        private void StoreData(Type type, Type dataType, IList data)
        {
            Lazy<object> lazyData;

            // this would, for example, be 'Bitcoin' or 'TradeBar'
            if (type == dataType)
            {
                lazyData = new Lazy<object>(() => data);
            }
            else
            {
                lazyData = new Lazy<object>(() => {
                    // common case where it's a List<BaseData> but dataType == TradeBar
                    // create a List<TradeBar> so that when accessed via GetAll<T> or .TradeBar
                    // the types line up as expected
                    var listType = GetGenericListType(dataType);
                    var list = (IList)Activator.CreateInstance(listType);
                    foreach (var datum in data)
                    {
                        // if the element type and dataType aren't in alignment we'll get an invalid cast exception here
                        list.Add(datum);
                    }

                    return list;});
            }

            SetProperty(dataType.Name, lazyData);
        }

        private Type GetGenericListType(Type type)
        {
            Type containerType;
            if (!_genericTypes.TryGetValue(type, out containerType))
            {
                // for performance we keep the generic type
                _genericTypes[type] = containerType = typeof(List<>).MakeGenericType(type);
            }

            return containerType;
        }
    }
}
