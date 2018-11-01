/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAsset : IAsset
    {
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public Guid AssetId { get; set; }

        [JsonProperty(PropertyName = "asset_class", Required = Required.Default)]
        public AssetClass Class { get; set; }

        [JsonProperty(PropertyName = "exchange", Required = Required.Always)]
        public Exchange Exchange { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public AssetStatus Status { get; set; }

        [JsonProperty(PropertyName = "tradable", Required = Required.Always)]
        public Boolean IsTradable { get; set; }
    }
}