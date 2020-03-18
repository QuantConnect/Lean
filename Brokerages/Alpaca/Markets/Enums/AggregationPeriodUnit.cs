/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Supported aggregation time windows for Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AggregationPeriodUnit
    {
        /// <summary>
        /// One minute window
        /// </summary>
        [EnumMember(Value = "minute")]
        Minute,

        /// <summary>
        /// One hour window
        /// </summary>
        [EnumMember(Value = "hour")]
        Hour,

        /// <summary>
        /// One day window
        /// </summary>
        [EnumMember(Value = "day")]
        Day,

        /// <summary>
        /// One week window
        /// </summary>
        [EnumMember(Value = "week")]
        Week,

        /// <summary>
        /// One month window
        /// </summary>
        [EnumMember(Value = "month")]
        Month,

        /// <summary>
        /// One quarter window
        /// </summary>
        [EnumMember(Value = "quarter")]
        Quarter,

        /// <summary>
        /// One year window
        /// </summary>
        [EnumMember(Value = "year")]
        Year,
    }
}
