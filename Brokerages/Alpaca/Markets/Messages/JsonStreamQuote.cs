/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal class JsonStreamQuote : IStreamQuote
    {
        [JsonProperty(PropertyName = "sym", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "bx", Required = Required.Always)]
        public Int64 BidExchange { get; set; }

        [JsonProperty(PropertyName = "ax", Required = Required.Always)]
        public Int64 AskExchange { get; set; }

        [JsonProperty(PropertyName = "bp", Required = Required.Always)]
        public Decimal BidPrice { get; set; }

        [JsonProperty(PropertyName = "ap", Required = Required.Always)]
        public Decimal AskPrice { get; set; }

        [JsonProperty(PropertyName = "bs", Required = Required.Always)]
        public Int64 BidSize { get; set; }

        [JsonProperty(PropertyName = "as", Required = Required.Always)]
        public Int64 AskSize { get; set; }

        [JsonProperty(PropertyName = "t", Required = Required.Always)]
        public Int64 Timestamp { get; set; }

        [JsonIgnore]
        public DateTime Time { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(
            StreamingContext context)
        {
            Time = DateTimeHelper.FromUnixTimeMilliseconds(Timestamp);
        }
    }
}
