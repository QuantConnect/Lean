using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAssetBars : IAssetBars
    {
        [JsonProperty(PropertyName = "asset_id", Required = Required.Always)]
        public Guid AssetId { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "exchange", Required = Required.Always)]
        public Exchange Exchange { get; set; }

        [JsonProperty(PropertyName = "asset_class", Required = Required.Always)]
        public AssetClass AssetClass { get; set; }

        [JsonProperty(PropertyName = "bars", Required = Required.Always)]
        public List<JsonBar> BarsList { get; set; }

        [JsonIgnore]
        public IReadOnlyList<IBar> Items => BarsList;
    }
}