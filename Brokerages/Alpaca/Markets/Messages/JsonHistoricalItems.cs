using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal abstract class JsonHistoricalItems<TApi, TJson> where TJson : TApi
    {
        [JsonProperty(PropertyName = "status", Required = Required.Default)]
        public String Status { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "ticks", Required = Required.Always)]
        public List<TJson> ItemsList { get; set; }

        [JsonIgnore]
        public IReadOnlyCollection<TApi> Items =>
            (IReadOnlyCollection<TApi>)ItemsList;
    }
}