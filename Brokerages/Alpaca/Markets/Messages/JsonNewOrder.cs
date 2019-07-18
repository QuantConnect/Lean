/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonNewOrder
    {
        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "qty", Required = Required.Always)]
        public Int64 Quantity { get; set; }

        [JsonProperty(PropertyName = "side", Required = Required.Always)]
        public OrderSide OrderSide { get; set; }

        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public OrderType OrderType { get; set; }

        [JsonProperty(PropertyName = "time_in_force", Required = Required.Always)]
        public TimeInForce TimeInForce { get; set; }

        [JsonProperty(PropertyName = "limit_price", Required = Required.Default)]
        public Decimal? LimitPrice { get; set; }

        [JsonProperty(PropertyName = "stop_price", Required = Required.Default)]
        public Decimal? StopPrice { get; set; }

        [JsonProperty(PropertyName = "client_order_id", Required = Required.Default)]
        public String ClientOrderId { get; set; }
    }
}
