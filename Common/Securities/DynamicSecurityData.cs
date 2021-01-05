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
        private readonly ConcurrentDictionary<Type, Type> _genericTypes = new ConcurrentDictionary<Type, Type>();
        private readonly SecurityCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicSecurityData"/> class
        /// </summary>
        /// <param name="registeredTypes">Provides all the registered data types for the algorithm</param>
        /// <param name="cache">The security cache</param>
        public DynamicSecurityData(IRegisteredSecurityDataTypesProvider registeredTypes, SecurityCache cache)
        {
            _registeredTypes = registeredTypes;
            _cache = cache;
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
            return _cache.HasData(typeof(T));
        }

        /// <summary>
        /// Gets whether or not this dynamic data instance has a property with the specified name.
        /// This is a case-insensitive search.
        /// </summary>
        /// <param name="name">The property name to check for</param>
        /// <returns>True if the property exists, false otherwise</returns>
        public bool HasProperty(string name)
        {
            Type type;
            if (_registeredTypes.TryGetType(name, out type))
            {
                return _cache.HasData(type);
            }
            return false;
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
        [Obsolete("DynamicSecurityData is a view of the SecurityCache. It is readonly, properties can not be set")]
        public object SetProperty(string name, object value)
        {
            throw new InvalidOperationException("DynamicSecurityData is a view of the SecurityCache. It is readonly, properties can not be set");
        }

        /// <summary>
        /// Gets the property's value with the specified name. This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to access</param>
        /// <returns>object value of BaseData</returns>
        public object GetProperty(string name)
        {
            // check to see if the requested name matches one of the algorithm registered data types and if
            // so, we'll return a new empty list. this precludes us from always needing to check HasData<T>
            Type type;
            if (_registeredTypes.TryGetType(name, out type))
            {
                IReadOnlyList<BaseData> data;
                if (_cache.TryGetValue(type, out data))
                {
                    return data;
                }

                var listType = GetGenericListType(type);
                return Activator.CreateInstance(listType);
            }

            throw new KeyNotFoundException($"Property with name '{name}' does not exist.");
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
                return list;
            }

            throw new InvalidOperationException(
                $"Expected a list with type '{type.GetBetterTypeName()}' " +
                $"but found type '{data.GetType().GetBetterTypeName()}"
            );
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
