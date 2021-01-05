/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonExchange : IExchange
    {
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public Int64 ExchangeId { get; set; }

        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public ExchangeType ExchangeType { get; set; }

        [JsonProperty(PropertyName = "market", Required = Required.Always)]
        public MarketDataType MarketDataType { get; set; }

        [JsonProperty(PropertyName = "mic", Required = Required.Default)]
        public String MarketIdentificationCode { get; set; }

        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public String Name { get; set; }

        [JsonProperty(PropertyName = "tape", Required = Required.Default)]
        public String TapeId { get; set; }
    }
}
