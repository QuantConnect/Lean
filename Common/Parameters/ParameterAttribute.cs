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
using System.Linq;
using System.Reflection;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Parameters
{
    /// <summary>
    /// Specifies a field or property is a parameter that can be set
    /// from an <see cref="AlgorithmNodePacket.Parameters"/> dictionary
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParameterAttribute : Attribute
    {
        /// <summary>
        /// Specifies the binding flags used by this implementation to resolve parameter attributes
        /// </summary>
        public const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;

        private static readonly string ParameterAttributeNameProperty = "Name";

        /// <summary>
        /// Gets the name of this parameter
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterAttribute"/> class
        /// </summary>
        /// <param name="name">The name of the parameter. If null is specified
        /// then the field or property name will be used</param>
        public ParameterAttribute(string name = null)
        {
            Name = name;
        }

        /// <summary>
        /// Uses reflections to inspect the instance for any parameter attributes.
        /// If a value is found in the parameters dictionary, it is set.
        /// </summary>
        /// <param name="parameters">The parameters dictionary</param>
        /// <param name="instance">The instance to set parameters on</param>
        public static void ApplyAttributes(Dictionary<string, string> parameters, object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            // get all fields/properties on the instance
            var members = type.GetFields(BindingFlags).Concat<MemberInfo>(type.GetProperties(BindingFlags));
            foreach (var memberInfo in members)
            {
                var fieldInfo = memberInfo as FieldInfo;
                var propertyInfo = memberInfo as PropertyInfo;

                // this line make static analysis a little happier, but should never actually throw
                if (fieldInfo == null && propertyInfo == null)
                {
                    throw new InvalidOperationException("Resolved member that is neither FieldInfo or PropertyInfo");
                }

                // check the member for our custom attribute
                var attribute = memberInfo.GetCustomAttribute<ParameterAttribute>();
                if (attribute == null) continue;

                // if no name is specified in the attribute then use the member name
                var parameterName = attribute.Name ?? memberInfo.Name;

                // get the parameter string value to apply to the member
                string parameterValue;
                if (!parameters.TryGetValue(parameterName, out parameterValue)) continue;

                // if it's a read-only property with a parameter value we can't really do anything, bail
                if (propertyInfo != null && !propertyInfo.CanWrite)
                {
                    var message = $"The specified property is read only: {propertyInfo.DeclaringType}.{propertyInfo.Name}";
                    throw new InvalidOperationException(message);
                }

                // resolve the member type
                var memberType = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;

                // convert the parameter string value to the member type
                var value = parameterValue.ConvertTo(memberType);

                // set the value to the field/property
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(instance, value);
                }
                else
                {
                    propertyInfo.SetValue(instance, value);
                }
            }
        }

        /// <summary>
        /// Resolves all parameter attributes from the specified compiled assembly path
        /// </summary>
        /// <param name="assembly">The assembly to inspect</param>
        /// <returns>Parameters dictionary keyed by parameter name with a value of the member type</returns>
        public static Dictionary<string, string> GetParametersFromAssembly(Assembly assembly)
        {
            var parameters = new Dictionary<string, string>();
            foreach (var type in assembly.GetTypes())
            {
                foreach (var kvp in GetParametersFromType(type))
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }
            return parameters;
        }

        /// <summary>
        /// Resolves all parameter attributes from the specified type
        /// </summary>
        /// <param name="type">The type to inspect</param>
        /// <returns>Parameters dictionary keyed by parameter name with a value of the member type</returns>
        public static IEnumerable<KeyValuePair<string, string>> GetParametersFromType(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags))
            {
                var attribute = field.GetCustomAttribute<ParameterAttribute>();
                if (attribute != null)
                {
                    var parameterName = attribute.Name ?? field.Name;
                    yield return new KeyValuePair<string, string>(parameterName, field.FieldType.GetBetterTypeName());
                }
            }

            foreach (var property in type.GetProperties(BindingFlags))
            {
                // ignore non-writeable properties
                if (!property.CanWrite) continue;
                var attribute = property.GetCustomAttribute<ParameterAttribute>();
                if (attribute != null)
                {
                    var parameterName = attribute.Name ?? property.Name;
                    yield return new KeyValuePair<string, string>(parameterName, property.PropertyType.GetBetterTypeName());
                }
            }
        }
    }
}
