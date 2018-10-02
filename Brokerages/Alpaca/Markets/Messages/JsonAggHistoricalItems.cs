/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonAggHistoricalItems<TApi, TJson>
        : JsonHistoricalItems<TApi, TJson>, IAggHistoricalItems<TApi> where TJson : TApi
    {
        [JsonProperty(PropertyName = "aggType", Required = Required.Always)]
        public AggregationType AggregationType { get; set; }
    }
}