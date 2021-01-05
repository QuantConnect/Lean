/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Historical data aggregation type in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AggregationType
    {
        /// <summary>
        /// Aggreagated data for single trading day.
        /// </summary>
        [EnumMember(Value = "daily")]
        Day,

        /// <summary>
        /// Aggregated data for single minute.
        /// </summary>
        [EnumMember(Value = "min")]
        Minute
    }
}
