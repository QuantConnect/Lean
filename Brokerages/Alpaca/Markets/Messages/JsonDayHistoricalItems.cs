/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    internal sealed class JsonDayHistoricalItems<TApi, TJson>
        : JsonHistoricalItems<TApi, TJson> , IDayHistoricalItems<TApi> where TJson : TApi
    {
        [JsonConverter(typeof(DateConverter))]
        [JsonProperty(PropertyName = "day", Required = Required.Always)]
        public DateTime ItemsDay { get; set; }

        [JsonProperty(PropertyName = "msLatency", Required = Required.Always)]
        public Int64 LatencyInMs { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(
            StreamingContext context)
        {
            ItemsDay = DateTime.SpecifyKind(
                ItemsDay.Date, DateTimeKind.Utc);
        }
    }
}