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
    /// Position side in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PositionSide
    {
        /// <summary>
        /// Long position.
        /// </summary>
        [EnumMember(Value = "long")]
        Long,

        /// <summary>
        /// Short position.
        /// </summary>
        [EnumMember(Value = "short")]
        Short
    }
}
