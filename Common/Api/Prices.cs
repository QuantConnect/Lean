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
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Api
{
    /// <summary>
    /// Prices rest response wrapper
    /// </summary>
    public class Prices
    {
        /// <summary>
        /// The requested Symbol
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The requested symbol ID
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public string SymbolID { get; set; }

        /// <summary>
        /// The requested price
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal Price { get; set; }

        /// <summary>
        /// UTC time the price was updated
        /// </summary>
        [JsonProperty(PropertyName = "updated"), JsonConverter(typeof(DoubleUnixSecondsDateTimeJsonConverter))]
        public DateTime Updated;
    }

    /// <summary>
    /// Collection container for a list of prices objects
    /// </summary>
    public class PricesList : RestResponse
    {
        /// <summary>
        /// Collection of prices objects
        /// </summary>
        [JsonProperty(PropertyName = "prices")]
        public List<Prices> Prices;
    }
}
