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
using Newtonsoft.Json.Linq;

namespace QuantConnect.Brokerages.Binance.Messages
{
    /// <summary>
    /// Deserializes cross margin Account data
    /// https://binance-docs.github.io/apidocs/spot/en/#query-cross-margin-account-details-user_data
    /// </summary>
    public class MarginAccountConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => typeof(AccountInformation).IsAssignableFrom(objectType);

        /// <summary>Reads the JSON representation of the margin account data and asset balances.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject token = JObject.Load(reader);
            var balances = token.GetValue("userAssets", StringComparison.OrdinalIgnoreCase).ToObject<MarginBalance[]>();
            if (balances == null)
            {
                throw new ArgumentException("userAssets parameter name is not specified.");
            }

            return new AccountInformation()
            {
                Balances = balances
            };
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
