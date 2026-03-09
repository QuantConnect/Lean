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
using System.Linq;
using System.Dynamic;
using Newtonsoft.Json;
using QuantConnect.Data;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Base generic dynamic command class
    /// </summary>
    public class Command : DynamicObject
    {
        private static readonly MethodInfo SetPropertyMethodInfo = typeof(Command).GetMethod("SetProperty");
        private static readonly MethodInfo GetPropertyMethodInfo = typeof(Command).GetMethod("GetProperty");

        private readonly Dictionary<string, object> _storage = new(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Useful to string representation in python
        /// </summary>
        protected string PayloadData { get; set; }

        /// <summary>
        /// Get the metaObject required for Dynamism.
        /// </summary>
        public sealed override DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new SerializableDynamicMetaObject(parameter, this, SetPropertyMethodInfo, GetPropertyMethodInfo);
        }

        /// <summary>
        /// Sets the property with the specified name to the value. This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to set</param>
        /// <param name="value">The new property value</param>
        /// <returns>Returns the input value back to the caller</returns>
        public object SetProperty(string name, object value)
        {
            if (value is JArray jArray)
            {
                return _storage[name] = jArray.ToObject<List<object>>();
            }
            else if (value is JObject jobject)
            {
                return _storage[name] = jobject.ToObject<Dictionary<string, object>>();
            }
            else
            {
                return _storage[name] = value;
            }
        }

        /// <summary>
        /// Gets the property's value with the specified name. This is a case-insensitve search.
        /// </summary>
        /// <param name="name">The property name to access</param>
        /// <returns>object value of BaseData</returns>
        public object GetProperty(string name)
        {
            if (!_storage.TryGetValue(name, out var value))
            {
                var type = GetType();
                if (type != typeof(Command))
                {
                    var propertyInfo = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo != null)
                    {
                        return propertyInfo.GetValue(this, null);
                    }
                    var fieldInfo = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        return fieldInfo.GetValue(this);
                    }
                }
                return null;
            }
            return value;
        }

        /// <summary>
        /// Run this command using the target algorithm
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>True if success, false otherwise. Returning null will disable command feedback</returns>
        public virtual bool? Run(IAlgorithm algorithm)
        {
            throw new NotImplementedException($"Please implement the 'def run(algorithm) -> bool | None:' method");
        }

        /// <summary>
        /// The string representation of this command
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PayloadData))
            {
                return PayloadData;
            }
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Helper class so we can serialize a command
        /// </summary>
        private class SerializableDynamicMetaObject : GetSetPropertyDynamicMetaObject
        {
            private readonly Command _object;
            public SerializableDynamicMetaObject(Expression expression, object value, MethodInfo setPropertyMethodInfo, MethodInfo getPropertyMethodInfo)
                : base(expression, value, setPropertyMethodInfo, getPropertyMethodInfo)
            {
                _object = (Command)value;
            }
            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return _object._storage.Keys.Concat(_object.GetType()
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property).Select(x => x.Name));
            }
        }
    }
}
