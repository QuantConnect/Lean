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
    /// Supported order durations in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TimeInForce
    {
        /// <summary>
        /// The order is good for the day, and it will be canceled automatically at the end of market hours.
        /// </summary>
        [EnumMember(Value = "day")]
        Day,

        /// <summary>
        /// The order is good until canceled.
        /// </summary>
        [EnumMember(Value = "gtc")]
        Gtc,

        /// <summary>
        /// The order is placed at the time the market opens.
        /// </summary>
        [EnumMember(Value = "opg")]
        Opg,

        /// <summary>
        /// The order is immediately filled or canceled after being placed (may partial fill).
        /// </summary>
        [EnumMember(Value = "ioc")]
        Ioc,

        /// <summary>
        /// The order is immediately filled or canceled after being placed (may not partial fill).
        /// </summary>
        [EnumMember(Value = "fok")]
        Fok
    }
}
