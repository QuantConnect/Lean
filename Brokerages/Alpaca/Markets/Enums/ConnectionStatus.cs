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
    /// Authorization status for Alpaca streaming API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum ConnectionStatus
    {
        /// <summary>
        /// Client successfully connected.
        /// </summary>
        [EnumMember(Value = "connected")]
        Connected,

        /// <summary>
        /// Client successfully authorized.
        /// </summary>
        [EnumMember(Value = "auth_success")]
        AuthenticationSuccess,

        /// <summary>
        /// Client authentication required.
        /// </summary>
        [EnumMember(Value = "auth_required")]
        AuthenticationRequired,

        /// <summary>
        /// Client authentication failed.
        /// </summary>
        [EnumMember(Value = "auth_failed")]
        AuthenticationFailed,

        /// <summary>
        /// Requested operation successfully completed.
        /// </summary>
        [EnumMember(Value = "success")]
        Success,

        /// <summary>
        /// Requested operation failed.
        /// </summary>
        [EnumMember(Value = "failed")]
        Failed
    }
}
