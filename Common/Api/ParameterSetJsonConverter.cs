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
using Newtonsoft.Json;
using QuantConnect.Util;
using System.Collections.Generic;
using QuantConnect.Optimizer.Parameters;

namespace QuantConnect.Api
{
    /// <summary>
    /// Json converter for <see cref="ParameterSet"/> which creates a light weight easy to consume serialized version
    /// </summary>
    public class ParameterSetJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ParameterSet);
        }

        /// <summary>
        /// Writes a JSON object from a Parameter set
        /// </summary>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var parameterSet = value as ParameterSet;
            if (ReferenceEquals(parameterSet, null)) return;

            writer.WriteStartObject();

            if (parameterSet.Value != null)
            {
                writer.WritePropertyName("parameterSet");
                serializer.Serialize(writer, parameterSet.Value);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                // {"parameterSet": parameters} or {name: value, ...}
                var parameterSet = new Dictionary<string, string>();
                while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
                {
                    var name = (string)reader.Value;
                    if (name == "parameterSet")
                    {
                        reader.Read();
                        parameterSet = ParameterDictionaryJsonConverter.Read(reader, serializer);
                    }
                    else
                    {
                        parameterSet[name] = reader.ReadAsString();
                    }
                }
                return new ParameterSet(-1, parameterSet);
            }

            var parameters = ParameterDictionaryJsonConverter.Read(reader, serializer);
            return parameters == null ? null : new ParameterSet(-1, parameters);
        }
    }
}
