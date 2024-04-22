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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect
{
    /// <summary>
    /// Defines a <see cref="JsonConverter"/> to be used when deserializing to
    /// the <see cref="Symbol"/> class.
    /// </summary>
    public class SymbolJsonConverter : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var symbol = value as Symbol;
            if (ReferenceEquals(symbol, null)) return;

            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(symbol.Value);
            writer.WritePropertyName("id");
            writer.WriteValue(symbol.ID.ToString());
            writer.WritePropertyName("permtick");
            writer.WriteValue(symbol.Value);
            if (symbol.HasUnderlying)
            {
                writer.WritePropertyName("underlying");
                WriteJson(writer, symbol.Underlying, serializer);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jobject = JObject.Load(reader);

            if (jobject.TryGetValue("type", StringComparison.InvariantCultureIgnoreCase, out var type))
            {
                return BuildSymbolFromUserFriendlyValue(jobject);
            }
            return ReadSymbolFromJson(jobject);
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (Symbol);
        }

        private Symbol ReadSymbolFromJson(JObject jObject)
        {
            JToken symbolId;
            JToken value;

            if ((jObject.TryGetValue("ID", StringComparison.InvariantCultureIgnoreCase, out symbolId)
                && jObject.TryGetValue("Value", StringComparison.InvariantCultureIgnoreCase, out value))
                || (jObject.TryGetValue("id", StringComparison.InvariantCultureIgnoreCase, out symbolId)
                && jObject.TryGetValue("value", StringComparison.InvariantCultureIgnoreCase, out value)))
            {
                Symbol underlyingSymbol = null;
                JToken underlying;
                if (jObject.TryGetValue("Underlying", StringComparison.InvariantCultureIgnoreCase, out underlying)
                    || jObject.TryGetValue("underlying", StringComparison.InvariantCultureIgnoreCase, out underlying))
                {
                    underlyingSymbol = ReadSymbolFromJson(underlying as JObject);
                }

                return new Symbol(SecurityIdentifier.Parse(symbolId.ToString()), value.ToString(), underlyingSymbol);
            }
            return null;
        }

        /// <summary>
        /// Creates a symbol from the user friendly string representation
        /// </summary>
        private Symbol BuildSymbolFromUserFriendlyValue(JObject jObject)
        {
            if (jObject.TryGetValue("value", StringComparison.InvariantCultureIgnoreCase, out var value)
                && jObject.TryGetValue("type", StringComparison.InvariantCultureIgnoreCase, out var securityTypeToken)
                && securityTypeToken.ToString().TryParseSecurityType(out var securityType))
            {
                if (securityType == SecurityType.Option)
                {
                    return SymbolRepresentation.ParseOptionTickerOSI(value.ToString());
                }
                else if (securityType == SecurityType.Future)
                {
                    return SymbolRepresentation.ParseFutureSymbol(value.ToString());
                }
                else if(securityType == SecurityType.FutureOption)
                {
                    return SymbolRepresentation.ParseFutureOptionSymbol(value.ToString());
                }
                else if (securityType == SecurityType.IndexOption)
                {
                    return SymbolRepresentation.ParseOptionTickerOSI(value.ToString(), securityType: securityType);
                }

                if (!jObject.TryGetValue("market", StringComparison.InvariantCultureIgnoreCase, out var market))
                {
                    market = Market.USA;
                }
                return Symbol.Create(value.ToString(), securityType, market.ToString());
            }
            return null;
        }
    }
}
