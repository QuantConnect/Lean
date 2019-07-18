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
    /// Supported asset types in Polygon REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MarketDataType
    {
        /// <summary>
        /// Equities.
        /// </summary>
        [EnumMember(Value = "equities")]
        Equities,

        /// <summary>
        /// Indexes.
        /// </summary>
        [EnumMember(Value = "index")]
        Indexes,

        /// <summary>
        /// Currencies.
        /// </summary>
        [EnumMember(Value = "currencies")]
        Currencies
    }
}
