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
    /// Single asset status in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AssetStatus
    {
        /// <summary>
        /// Active asset.
        /// </summary>
        [EnumMember(Value = "active")]
        Active,

        /// <summary>
        /// Inactive asset.
        /// </summary>
        [EnumMember(Value = "inactive")]
        Inactive,

        /// <summary>
        /// Delisted asset.
        /// </summary>
        [EnumMember(Value = "delisted")]
        Delisted
    }
}
