/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class JsonPortfolioHistory : IPortfolioHistory
    {
        private sealed class Item : IPortfolioHistoryItem
        {
            public Decimal? Equity { get; set; }

            public Decimal? ProfitLoss { get; set; }

            public Decimal? ProfitLossPercentage { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private readonly List<IPortfolioHistoryItem> _items = new List<IPortfolioHistoryItem>();

        [JsonProperty(PropertyName = "equity", Required = Required.Always)]
        public List<Decimal?> EquityList { get; set; }

        [JsonProperty(PropertyName = "profit_loss", Required = Required.Always)]
        public List<Decimal?> ProfitLossList { get; set; }

        [JsonProperty(PropertyName = "profit_loss_pct", Required = Required.Always)]
        public List<Decimal?> ProfitLossPercentageList { get; set; }

        [JsonProperty(PropertyName = "timestamp", Required = Required.Always)]
        public List<Int64> TimestampsList { get; set; }

        [JsonIgnore]
        public IReadOnlyList<IPortfolioHistoryItem> Items => _items;

        [JsonProperty(PropertyName = "timeframe", Required = Required.Always)]
        public TimeFrame TimeFrame { get; set; }

        [JsonProperty(PropertyName = "base_value", Required = Required.Always)]
        public Decimal BaseValue { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(
            StreamingContext context)
        {
            var equities = EquityList.EmptyIfNull();
            var timestamps = TimestampsList.EmptyIfNull();
            var profitLosses = ProfitLossList.EmptyIfNull();
            var profitLossesPercentage = ProfitLossPercentageList.EmptyIfNull();

            var count = Math.Min(
                Math.Min(equities.Count, timestamps.Count),
                Math.Min(profitLosses.Count, profitLossesPercentage.Count));

            for (var index = 0; index < count; ++index)
            {
                _items.Add(new Item()
                {
                    Equity = equities[index],
                    ProfitLoss = profitLosses[index],
                    ProfitLossPercentage = profitLossesPercentage[index],
                    Timestamp = DateTimeHelper.FromUnixTimeSeconds(timestamps[index]),
                });
            }
        }
    }
}
