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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IRegisteredSecurityDataTypesProvider"/> that permits the
    /// consumer to modify the expected types
    /// </summary>
    public class RegisteredSecurityDataTypesProvider : IRegisteredSecurityDataTypesProvider
    {
        /// <summary>
        /// Provides a reference to an instance of <see cref="IRegisteredSecurityDataTypesProvider"/> that contains no registered types
        /// </summary>
        public static readonly IRegisteredSecurityDataTypesProvider Null = new RegisteredSecurityDataTypesProvider();

        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

        /// <summary>
        /// Registers the specified type w/ the provider
        /// </summary>
        /// <returns>True if the type was previously not registered</returns>
        public bool RegisterType(Type type)
        {
            Type existingType;
            if (_types.TryGetValue(type.Name, out existingType))
            {
                if (existingType != type)
                {
                    // shouldn't happen but we want to know if it does
                    throw new InvalidOperationException(
                        $"Two different types were detected trying to register the same type name: {existingType} - {type}");
                }
                return true;
            }

            _types[type.Name] = type;
            return false;
        }

        /// <summary>
        /// Removes the registration for the specified type
        /// </summary>
        /// <returns>True if the type was previously registered</returns>
        public bool UnregisterType(Type type)
        {
            return _types.Remove(type.Name);
        }

        /// <summary>
        /// Gets an enumerable of data types expected to be contained in a <see cref="DynamicSecurityData"/> instance
        /// </summary>
        public bool TryGetType(string name, out Type type)
        {
            if (!_types.TryGetValue(name, out type))
            {
                // lets try a case insensitive search
                var kvp = _types.FirstOrDefault(pair => pair.Key.ToLowerInvariant() == name.ToLowerInvariant());
                if (kvp.Equals(default(KeyValuePair<string, Type>)))
                {
                    return false;
                }

                type = kvp.Value;
                return true;
            }
            return true;
        }
    }
}