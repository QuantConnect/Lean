/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonOrder : IOrder
    {
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public Guid OrderId { get; set; }

        [JsonProperty(PropertyName = "client_order_id", Required = Required.Always)]
        public String ClientOrderId { get; set; }

        [JsonProperty(PropertyName = "created_at", Required = Required.Default)]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty(PropertyName = "updated_at", Required = Required.Default)]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty(PropertyName = "submitted_at", Required = Required.Default)]
        public DateTime? SubmittedAt { get; set; }

        [JsonProperty(PropertyName = "filled_at", Required = Required.Default)]
        public DateTime? FilledAt { get; set; }

        [JsonProperty(PropertyName = "expired_at ", Required = Required.Default)]
        public DateTime? ExpiredAt { get; set; }

        [JsonProperty(PropertyName = "canceled_at", Required = Required.Default)]
        public DateTime? CancelledAt { get; set; }

        [JsonProperty(PropertyName = "failed_at", Required = Required.Default)]
        public DateTime? FailedAt { get; set; }

        [JsonProperty(PropertyName = "asset_id", Required = Required.Always)]
        public Guid AssetId { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "asset_class", Required = Required.Always)]
        public AssetClass AssetClass { get; set; }

        [JsonProperty(PropertyName = "qty", Required = Required.Always)]
        public Int64 Quantity { get; set; }

        [JsonProperty(PropertyName = "filled_qty", Required = Required.Always)]
        public Int64 FilledQuantity { get; set; }

        [JsonProperty(PropertyName = "order_type", Required = Required.Always)]
        public OrderType OrderType { get; set; }

        [JsonProperty(PropertyName = "side", Required = Required.Always)]
        public OrderSide OrderSide { get; set; }

        [JsonProperty(PropertyName = "time_in_force", Required = Required.Always)]
        public TimeInForce TimeInForce { get; set; }

        [JsonProperty(PropertyName = "limit_price", Required = Required.Default)]
        public Decimal? LimitPrice { get; set; }

        [JsonProperty(PropertyName = "stop_price", Required = Required.Default)]
        public Decimal? StopPrice { get; set; }

        [JsonProperty(PropertyName = "filled_avg_price", Required = Required.Default)]
        public Decimal? AverageFillPrice { get; set; }

        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public OrderStatus OrderStatus { get; set; }
    }
}
