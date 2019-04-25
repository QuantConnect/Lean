/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonQuote : IQuote
    {
        [JsonProperty(PropertyName = "asset_id", Required = Required.Always)]
        public Guid AssetId { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "asset_class", Required = Required.Always)]
        public AssetClass AssetClass { get; set; }

        [JsonProperty(PropertyName = "bid", Required = Required.Always)]
        public Decimal BidPrice { get; set; }

        [JsonProperty(PropertyName = "bid_timestamp", Required = Required.Always)]
        public DateTime BidTime { get; set; }

        [JsonProperty(PropertyName = "ask", Required = Required.Always)]
        public Decimal AskPrice { get; set; }

        [JsonProperty(PropertyName = "ask_timestamp", Required = Required.Always)]
        public DateTime AskTime { get; set; }

        [JsonProperty(PropertyName = "last", Required = Required.Always)]
        public Decimal LastPrice { get; set; }

        [JsonProperty(PropertyName = "last_timestamp", Required = Required.Always)]
        public DateTime LastTime { get; set; }

        [JsonProperty(PropertyName = "day_change", Required = Required.Default)]
        public Decimal DayChange { get; set; }
    }
}