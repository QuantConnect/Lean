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

namespace QuantConnect.ToolBox.CoinApi.Messages
{
    public class HistoricalDataMessage
    {
        [JsonProperty("time_period_start")]
        public DateTime TimePeriodStart { get; set; }

        [JsonProperty("time_period_end")]
        public DateTime TimePeriodEnd { get; set; }

        [JsonProperty("price_open")]
        public decimal PriceOpen { get; set; }

        [JsonProperty("price_high")]
        public decimal PriceHigh { get; set; }

        [JsonProperty("price_low")]
        public decimal PriceLow { get; set; }

        [JsonProperty("price_close")]
        public decimal PriceClose { get; set; }

        [JsonProperty("volume_traded")]
        public decimal VolumeTraded { get; set; }

        [JsonProperty("trades_count")]
        public int TradesCount { get; set; }
    }
}
