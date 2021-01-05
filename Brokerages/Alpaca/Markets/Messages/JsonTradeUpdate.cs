/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonTradeUpdate : ITradeUpdate
    {
        [JsonProperty(PropertyName = "event", Required = Required.Always)]
        public TradeEvent Event { get; set; }

        [JsonProperty(PropertyName = "price", Required = Required.Default)]
        public Decimal? Price { get; set; }

        [JsonProperty(PropertyName = "qty", Required = Required.Default)]
        public Int64? Quantity { get; set; }

        [JsonProperty(PropertyName = "timestamp", Required = Required.Default)]
        public DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "order", Required = Required.Always)]
        public JsonOrder JsonOrder { get; set; }

        public IOrder Order => JsonOrder;
    }
}
