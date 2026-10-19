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
using QuantConnect.Api;
using System.Collections.Generic;

namespace QuantConnect.Util
{
    /// <summary>
    /// Reads a parameter name to value dictionary from either the {name: value} or the [{"key": key, "value": value}] format
    /// </summary>
    public class ParameterDictionaryJsonConverter : JsonConverter
    {
        /// <summary>
        /// Writes are handled by the default serializer, using the {name: value} format
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Determines whether this instance can convert the specified object type
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, string>);
        }

        /// <summary>
        /// Reads the parameters dictionary from the JSON
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return Read(reader, serializer);
        }

        /// <summary>
        /// Not used, see <see cref="CanWrite"/>
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the parameters dictionary at the reader's current token, null if the token is null.
        /// Values are read as strings, as written, so dates, decimals and booleans keep their format
        /// </summary>
        internal static Dictionary<string, string> Read(JsonReader reader, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;

                case JsonToken.StartObject:
                    // {name: value, ...}
                    return serializer.Deserialize<Dictionary<string, string>>(reader);

                case JsonToken.StartArray:
                    // [{"key": key, "value": value}, ...], the optimization properties (min, max, step) don't apply here
                    var parameters = new Dictionary<string, string>();
                    foreach (var parameter in serializer.Deserialize<List<Parameter>>(reader))
                    {
                        if (!string.IsNullOrEmpty(parameter?.Name))
                        {
                            parameters[parameter.Name] = parameter.Value;
                        }
                    }
                    return parameters;

                default:
                    throw new ArgumentException($"Unexpected Tokentype {reader.TokenType}");
            }
        }
    }
}
