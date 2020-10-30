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
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Optimizer
{
    /// <summary>
    /// Override <see cref="OptimizationParameter"/> deserialization method
    /// </summary>
    public class OptimizationParameterConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var optimizationParameter = value as OptimizationParameter;
            if (ReferenceEquals(optimizationParameter, null))
            {
                writer.WriteNull(); 
                return;
            }

            var master = new JObject();

            var type = value.GetType();
            var namProperty = type.GetProperties().FirstOrDefault(s =>
                string.Equals(s.Name, "name", StringComparison.OrdinalIgnoreCase));

            if (namProperty != null)
            {
                var name = namProperty.GetValue(value, null);
                if (name != null)
                {
                    var jo = new JObject();
                    master.Add(Convert.ToString(name), jo);

                    foreach (var property in type.GetProperties())
                    {
                        if (property.CanRead && !string.Equals(property.Name, "name", StringComparison.OrdinalIgnoreCase))
                        {
                            var attribute = property.GetCustomAttribute<JsonPropertyAttribute>();
                            var propertyName = attribute != null
                                ? attribute.PropertyName
                                : property.Name;
                            var propertyValue = property.GetValue(value, null);
                            if (propertyValue != null)
                            {
                                jo.Add(propertyName, JToken.FromObject(propertyValue, serializer));
                            }
                        }
                    }

                    master.WriteTo(writer);
                }
            }
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
            )
        {
            var parameterName = reader.Path;
            JToken token;
            switch (reader.TokenType)
            {
                case JsonToken.StartArray:
                    token = JToken.Load(reader);
                    List<string> items = token.ToObject<List<string>>();
                    return new OptimizationArrayParameter(parameterName,
                        items);
                case JsonToken.StartObject:
                    token = JToken.Load(reader);
                    return new OptimizationStepParameter(parameterName,
                        token.Value<decimal>("min"),
                        token.Value<decimal>("max"),
                        token.Value<decimal>("step"));
                default:
                    throw new ArgumentException(
                        $"Optimization parameters of type {reader.TokenType} are not currently supported.");
            }
        }

        public override bool CanConvert(Type objectType) => typeof(OptimizationParameter).IsAssignableFrom(objectType);
    }
}
