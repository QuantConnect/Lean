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
    /// Supported order types in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderType
    {
        /// <summary>
        /// Market order (no prices required).
        /// </summary>
        [EnumMember(Value = "market")]
        Market,

        /// <summary>
        /// Stop order (stop price required).
        /// </summary>
        [EnumMember(Value = "stop")]
        Stop,

        /// <summary>
        /// Limit order (limit price required).
        /// </summary>
        [EnumMember(Value = "limit")]
        Limit,

        /// <summary>
        /// Stop limit order (both stop and limit prices required).
        /// </summary>
        [EnumMember(Value = "stop_limit")]
        StopLimit
    }
}
