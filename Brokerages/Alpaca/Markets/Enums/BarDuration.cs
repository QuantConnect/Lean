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
    /// Historical bar duration size for Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BarDuration
    {
        /// <summary>
        /// Minute bar.
        /// </summary>
        [EnumMember(Value = "1Min")]
        OneMinute,

        /// <summary>
        /// Five minutes bar.
        /// </summary>
        [EnumMember(Value = "5Min")]
        FiveMinutes,

        /// <summary>
        /// Fifteen minutes bar.
        /// </summary>
        [EnumMember(Value = "15Min")]
        FifteenMinutes,

        /// <summary>
        /// Hourly bar.
        /// </summary>
        [EnumMember(Value = "1H")]
        OneHour,

        /// <summary>
        /// Daily bar.
        /// </summary>
        [EnumMember(Value = "1D")]
        OneDay,
    }
}