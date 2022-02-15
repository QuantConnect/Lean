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

namespace QuantConnect.ToolBox.GDAXDownloader.Models
{
    /// <summary>
    /// Represents GDAX exchange info for a product
    /// </summary>
    public class Product
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "base_currency")]
        public string BaseCurrency { get; set; }

        [JsonProperty(PropertyName = "quote_currency")]
        public string QuoteCurrency { get; set; }

        [JsonProperty(PropertyName = "base_min_size")]
        public string BaseMinSize { get; set; }

        [JsonProperty(PropertyName = "base_max_size")]
        public string BaseMaxSize { get; set; }

        [JsonProperty(PropertyName = "quote_increment")]
        public string QuoteIncrement { get; set; }

        [JsonProperty(PropertyName = "base_increment")]
        public string BaseIncrement { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "min_market_funds")]
        public string MinMarketFunds { get; set; }

        [JsonProperty(PropertyName = "max_market_funds")]
        public string MaxMarketFunds { get; set; }

        [JsonProperty(PropertyName = "margin_enabled")]
        public bool MarginEnabled { get; set; }

        [JsonProperty(PropertyName = "post_only")]
        public bool PostOnly { get; set; }

        [JsonProperty(PropertyName = "limit_only")]
        public bool LimitOnly { get; set; }

        [JsonProperty(PropertyName = "cancel_only")]
        public bool CancelOnly { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "status_message")]
        public string StatusMessage { get; set; }

        [JsonProperty(PropertyName = "auction_mode")]
        public bool AuctionMode { get; set; }
    }
}
