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
    /// REST API version number.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApiVersion
    {
        /// <summary>
        /// First version number.
        /// </summary>
        [EnumMember(Value = "v1")]
        V1 = 1,

        /// <summary>
        /// Second version number.
        /// </summary>
        [EnumMember(Value = "v2")]
        V2 = 2
    }
}
