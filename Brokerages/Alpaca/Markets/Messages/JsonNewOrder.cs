/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonNewOrder
    {
        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; } = String.Empty;

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

        [JsonProperty(PropertyName = "extended_hours", Required = Required.Default)]
        public Boolean? ExtendedHours { get; set; }

        [JsonProperty(PropertyName = "order_class", Required = Required.Default)]
        public OrderClass? OrderClass { get; set; }

        [JsonProperty(PropertyName = "take_profit", Required = Required.Default)]
        public JsonNewOrderAdvancedAttributes TakeProfit { get; set; }

        [JsonProperty(PropertyName = "stop_loss", Required = Required.Default)]
        public JsonNewOrderAdvancedAttributes StopLoss { get; set; }
    }
}
