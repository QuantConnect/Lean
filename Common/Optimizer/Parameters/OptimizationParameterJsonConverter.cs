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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Util;
using System;
using System.Reflection;

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Override <see cref="OptimizationParameter"/> deserialization method.
    /// Can handle <see cref="OptimizationStepParameter"/> instances
    /// </summary>
    public class OptimizationParameterJsonConverter : JsonConverter
    {
        /// <summary>
        /// Writes a JSON object from a OptimizationParameter object
        /// </summary>        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jo = new JObject();
            Type type = value.GetType();

            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.CanRead)
                {
                    var attribute = prop.GetCustomAttribute<JsonPropertyAttribute>();
                    object propVal = prop.GetValue(value, null);
                    if (propVal != null)
                    {
                        jo.Add(attribute.PropertyName ?? prop.Name, JToken.FromObject(propVal, serializer));
                    }
                }
            }
            jo.WriteTo(writer);
        }

        /// <summary>
        /// Creates a Optimization Parameter object from a JSON object
        /// </summary>
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
            )
        {
            JObject token = JObject.Load(reader);
            var parameterName = ParameterDictionaryJsonConverter.GetName(token);
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Messages.OptimizationParameterJsonConverter.OptimizationParameterNotSpecified);
            }

            JToken value;
            // optional properties can be missing or null
            var min = GetDecimal(token, "min");
            var max = GetDecimal(token, "max");
            OptimizationParameter optimizationParameter = null;
            if (min.HasValue && max.HasValue)
            {
                var step = GetDecimal(token, "step");
                var minStep = GetDecimal(token, "minStep") ?? GetDecimal(token, "min-step");
                if (step.HasValue)
                {
                    if (minStep.HasValue)
                    {
                        optimizationParameter = new OptimizationStepParameter(parameterName, min.Value, max.Value, step.Value, minStep.Value);
                    }
                    else
                    {
                        optimizationParameter = new OptimizationStepParameter(parameterName, min.Value, max.Value, step.Value);
                    }
                }
                else
                {
                    optimizationParameter = new OptimizationStepParameter(parameterName, min.Value, max.Value);
                }
            }
            else if(token.TryGetValue("value", StringComparison.OrdinalIgnoreCase, out value))
            {
                optimizationParameter = new StaticOptimizationParameter(parameterName, value.Value<string>());
            }

            if (optimizationParameter == null)
            {
                throw new ArgumentException(Messages.OptimizationParameterJsonConverter.OptimizationParameterNotSupported);
            }

            return optimizationParameter;
        }

        /// <summary>
        /// Determines if an OptimizationParameter is assignable from the given object type
        /// </summary>
        public override bool CanConvert(Type objectType) => typeof(OptimizationParameter).IsAssignableFrom(objectType);

        private static decimal? GetDecimal(JObject parameter, string propertyName)
        {
            return parameter.GetValue(propertyName, StringComparison.OrdinalIgnoreCase)?.Value<decimal?>();
        }
    }
}
