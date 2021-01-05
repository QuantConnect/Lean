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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.ToolBox.BinanceDownloader.Models
{
    /// <summary>
    /// Represents Binance exchange info for symbol
    /// </summary>
    public class Symbol
    {
        [JsonProperty(PropertyName = "symbol")]
        public string Name { get; set; }

        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }

        public bool IsSpotTradingAllowed { get; set; }
        public bool IsMarginTradingAllowed { get; set; }

        /// <summary>
        /// Exchange info filter defines trading rules on a symbol or an exchange
        /// https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md#filters
        /// </summary>
        public JObject[] Filters { get; set; }

        public string[] Permissions { get; set; }
    }
}
