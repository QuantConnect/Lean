/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal class JsonHistoricalTrade : IHistoricalTrade
    {
        [JsonProperty(PropertyName = "e", Required = Required.Always)]
        public String Exchange { get; set; }

        [JsonProperty(PropertyName = "t", Required = Required.Always)]
        public Int64 TimeOffset { get; set; }

        [JsonProperty(PropertyName = "p", Required = Required.Always)]
        public Decimal Price { get; set; }

        [JsonProperty(PropertyName = "s", Required = Required.Default)]
        public Int64 Size { get; set; }
    }
}