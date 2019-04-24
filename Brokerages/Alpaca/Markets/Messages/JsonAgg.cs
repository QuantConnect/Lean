/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAgg : IAgg
    {
        [JsonProperty(PropertyName = "open", Required = Required.Always)]
        public Decimal Open { get; set; }

        [JsonProperty(PropertyName = "high", Required = Required.Always)]
        public Decimal High { get; set; }

        [JsonProperty(PropertyName = "low", Required = Required.Always)]
        public Decimal Low { get; set; }

        [JsonProperty(PropertyName = "close", Required = Required.Always)]
        public Decimal Close { get; set; }

        [JsonProperty(PropertyName = "volume", Required = Required.Always)]
        public Int64 Volume { get; set; }

        [JsonProperty(PropertyName = "time", Required = Required.Always)]
        public DateTime Time { get; set; }
    }
}
