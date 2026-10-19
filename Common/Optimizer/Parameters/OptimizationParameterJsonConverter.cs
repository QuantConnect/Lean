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
            var parameter = serializer.Deserialize<OptimizationParameterProperties>(reader);
            var parameterName = parameter?.Name ?? parameter?.Key;
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Messages.OptimizationParameterJsonConverter.OptimizationParameterNotSpecified);
            }

            OptimizationParameter optimizationParameter = null;
            if (parameter.Min.HasValue && parameter.Max.HasValue)
            {
                var min = parameter.Min.Value;
                var max = parameter.Max.Value;
                var minStep = parameter.MinStep ?? parameter.MinStepDashed;
                if (parameter.Step.HasValue)
                {
                    if (minStep.HasValue)
                    {
                        optimizationParameter = new OptimizationStepParameter(parameterName, min, max, parameter.Step.Value, minStep.Value);
                    }
                    else
                    {
                        optimizationParameter = new OptimizationStepParameter(parameterName, min, max, parameter.Step.Value);
                    }
                }
                else
                {
                    optimizationParameter = new OptimizationStepParameter(parameterName, min, max);
                }
            }
            else if (parameter.HasValue)
            {
                optimizationParameter = new StaticOptimizationParameter(parameterName, parameter.Value);
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

        /// <summary>
        /// The optimization parameter properties as written, the optional ones can be missing or null
        /// </summary>
        private class OptimizationParameterProperties
        {
            private string _value;

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("min")]
            public decimal? Min { get; set; }

            [JsonProperty("max")]
            public decimal? Max { get; set; }

            [JsonProperty("step")]
            public decimal? Step { get; set; }

            [JsonProperty("minStep")]
            public decimal? MinStep { get; set; }

            [JsonProperty("min-step")]
            public decimal? MinStepDashed { get; set; }

            [JsonProperty("value")]
            public string Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    HasValue = true;
                }
            }

            /// <summary>
            /// True if the value property was set, even to null
            /// </summary>
            [JsonIgnore]
            public bool HasValue { get; private set; }
        }
    }
}
