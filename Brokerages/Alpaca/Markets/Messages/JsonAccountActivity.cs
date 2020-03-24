/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class JsonAccountActivity : IAccountActivity
    {
        [JsonProperty(PropertyName = "activity_type", Required = Required.Always)]
        public AccountActivityType ActivityType { get; set; }

        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public String ActivityId { get; set; } = String.Empty;

        [JsonProperty(PropertyName = "symbol", Required = Required.Default)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "date", Required = Required.Default)]
        public DateTime? ActivityDate { get; set; }

        [JsonProperty(PropertyName = "net_amount", Required = Required.Default)]
        public Decimal? NetAmount { get; set; }

        [JsonProperty(PropertyName = "qty", Required = Required.Default)]
        public Int64? Quantity { get; set; }

        [JsonProperty(PropertyName = "per_share_amount", Required = Required.Default)]
        public Decimal? PerShareAmount { get; set; }

        [JsonProperty(PropertyName = "cum_qty", Required = Required.Default)]
        public Int64? CumulativeQuantity { get; set; }

        [JsonProperty(PropertyName = "leaves_qty", Required = Required.Default)]
        public Int64? LeavesQuantity { get; set; }

        [JsonProperty(PropertyName = "price", Required = Required.Default)]
        public Decimal? Price { get; set; }

        [JsonProperty(PropertyName = "side", Required = Required.Default)]
        public OrderSide? Side { get; set; }

        [JsonProperty(PropertyName = "transaction_time", Required = Required.Default)]
        public DateTime? TransactionTime { get; set; }

        [JsonProperty(PropertyName = "type", Required = Required.Default)]
        public TradeEvent? Type { get; set; }

        [JsonIgnore]
        public DateTime ActivityDateTime { get; set; }

        [JsonIgnore]
        public Guid ActivityGuid { get; set; }
    }
}
