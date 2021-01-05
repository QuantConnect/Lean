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
    /// Authorization status for Alpaca streaming API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthStatus
    {
        /// <summary>
        /// Client successfully authorized.
        /// </summary>
        [EnumMember(Value = "authorized")]
        Authorized,

        /// <summary>
        /// Client does not authorized.
        /// </summary>
        [EnumMember(Value = "unauthorized")]
        Unauthorized
    }
}
