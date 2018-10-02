/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Supported order durations in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TimeInForce
    {
        /// <summary>
        /// Daily order.
        /// </summary>
        [EnumMember(Value = "day")]
        Day,

        /// <summary>
        /// Good-till-cancal order.
        /// </summary>
        [EnumMember(Value = "gtc")]
        Gtc,

        /// <summary>
        /// Market-on-open order.
        /// </summary>
        [EnumMember(Value = "opg")]
        Opg
    }
}