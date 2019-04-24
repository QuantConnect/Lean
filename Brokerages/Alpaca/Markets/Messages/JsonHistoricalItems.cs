/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal abstract class JsonHistoricalItems<TApi, TJson> where TJson : TApi
    {
        private static readonly IReadOnlyList<TApi> _empty = new TApi[0];

        [JsonProperty(PropertyName = "status", Required = Required.Default)]
        public String Status { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "ticks", Required = Required.Default)]
        public List<TJson> ItemsList { get; set; }

        [JsonIgnore]
        public IReadOnlyList<TApi> Items =>
            (IReadOnlyList<TApi>)ItemsList ?? _empty;
    }
}
