/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updates from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    // TODO: OlegRa - remove `V1` class and flatten hierarchy after removing Polygon Historical API v1 support

    internal abstract class JsonHistoricalItemsBase<TApi, TJson> : IHistoricalItems<TApi> where TJson : TApi
    {
        [JsonProperty(PropertyName = "status", Required = Required.Default)]
        public String Status { get; set; }

        public abstract String Symbol { get; set; }

        [JsonProperty(PropertyName = "adjusted", Required = Required.Default)]
        public Boolean Adjusted { get; set; }

        [JsonProperty(PropertyName = "queryCount", Required = Required.Default)]
        public Int64 QueryCount { get; set; }

        [JsonProperty(PropertyName = "resultsCount", Required = Required.Default)]
        public Int64 ResultsCount { get; set; }

        public abstract Int64 DbLatencyInMilliseconds { get; set; }

        public abstract List<TJson> ItemsList { get; set; }

        [JsonIgnore]
        public TimeSpan DatabaseLatency =>
            TimeSpan.FromMilliseconds(DbLatencyInMilliseconds);

        [JsonIgnore]
        public IReadOnlyList<TApi> Items => ItemsList.EmptyIfNull<TApi, TJson>();
    }

    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal class JsonHistoricalItems<TApi, TJson>
        : JsonHistoricalItemsBase<TApi, TJson> where TJson : TApi
    {
        [JsonProperty(PropertyName = "ticker", Required = Required.Default)]
        public override String Symbol { get; set; }

        [JsonProperty(PropertyName = "db_latency", Required = Required.Default)]
        public override Int64 DbLatencyInMilliseconds { get; set; }

        [JsonProperty(PropertyName = "results", Required = Required.Default)]
        public override List<TJson> ItemsList { get; set; }
    }

    internal abstract class JsonHistoricalItemsV1<TApi, TJson>
        : JsonHistoricalItemsBase<TApi, TJson> where TJson : TApi
    {
        [JsonProperty(PropertyName = "symbol", Required = Required.Default)]
        public override String Symbol { get; set; }

        [JsonProperty(PropertyName = "msLatency", Required = Required.Default)]
        public override Int64 DbLatencyInMilliseconds { get; set; }

        [JsonProperty(PropertyName = "ticks", Required = Required.Default)]
        public override List<TJson> ItemsList { get; set; }
    }
}