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

namespace QuantConnect.Data.Custom.Benzinga
{
    /// <summary>
    /// Attaches sentiment to a Symbol.
    /// </summary>
    /// <remarks>Not all Benzinga tickers have sentiment</remarks>
    public class BenzingaSymbolData
    {
        /// <summary>
        /// Sentiment of the news article. Ranges from -1.0 to 1.0
        /// </summary>
        [JsonProperty("@sentiment")]
        public decimal? Sentiment { get; set; }

        /// <summary>
        /// Exchange the symbol belongs to
        /// </summary>
        [JsonProperty("@exchange")]
        public string Exchange { get; set; }

        /// <summary>
        /// Symbol
        /// </summary>
        [JsonProperty("#text")]
        public Symbol Symbol { get; set; }
    }
}
