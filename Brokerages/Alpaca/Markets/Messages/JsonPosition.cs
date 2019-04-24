/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonPosition : IPosition
    {
        [JsonProperty(PropertyName = "account_id", Required = Required.Default)]
        public Guid AccountId { get; set; }

        [JsonProperty(PropertyName = "asset_id", Required = Required.Always)]
        public Guid AssetId { get; set; }

        [JsonProperty(PropertyName = "symbol", Required = Required.Always)]
        public String Symbol { get; set; }

        [JsonProperty(PropertyName = "exchange", Required = Required.Always)]
        public Exchange Exchange { get; set; }

        [JsonProperty(PropertyName = "asset_class", Required = Required.Always)]
        public AssetClass AssetClass { get; set; }

        [JsonProperty(PropertyName = "avg_entry_price", Required = Required.Always)]
        public Decimal AverageEntryPrice { get; set; }

        [JsonProperty(PropertyName = "qty", Required = Required.Always)]
        public Int32 Quantity { get; set; }

        [JsonProperty(PropertyName = "side", Required = Required.Default)]
        public PositionSide Side { get; set; }

        [JsonProperty(PropertyName = "market_value", Required = Required.Always)]
        public Decimal MarketValue { get; set; }

        [JsonProperty(PropertyName = "cost_basis", Required = Required.Always)]
        public Decimal CostBasis { get; set; }

        [JsonProperty(PropertyName = "unrealized_pl", Required = Required.Always)]
        public Decimal UnrealizedProfitLoss { get; set; }

        [JsonProperty(PropertyName = "unrealized_plpc", Required = Required.Always)]
        public Decimal UnrealizedProfitLossPercent { get; set; }

        [JsonProperty(PropertyName = "unrealized_intraday_pl", Required = Required.Always)]
        public Decimal IntradayUnrealizedProfitLoss { get; set; }

        [JsonProperty(PropertyName = "unrealized_intraday_plpc", Required = Required.Always)]
        public Decimal IntradayUnrealizedProfitLossPercent { get; set; }

        [JsonProperty(PropertyName = "current_price", Required = Required.Default)]
        public Decimal AssetCurrentPrice { get; set; }

        [JsonProperty(PropertyName = "lastday_price", Required = Required.Always)]
        public Decimal AssetLastPrice { get; set; }

        [JsonProperty(PropertyName = "change_today", Required = Required.Always)]
        public Decimal AssetChangePercent { get; set; }
    }
}
