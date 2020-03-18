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
    /// Period units for portfolio history in the Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HistoryPeriodUnit
    {
        /// <summary>
        /// Day
        /// </summary>
        [EnumMember(Value = "D")]
        Day,

        /// <summary>
        /// Month
        /// </summary>
        [EnumMember(Value = "W")]
        Week,

        /// <summary>
        /// Month
        /// </summary>
        [EnumMember(Value = "M")]
        Month,

        /// <summary>
        /// 3 month
        /// </summary>
        [EnumMember(Value = "A")]
        Year
    }
}
