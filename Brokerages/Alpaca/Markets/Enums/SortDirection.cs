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
    /// Supported sort directions in Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SortDirection
    {
        /// <summary>
        /// Descending sort order
        /// </summary>
        [EnumMember(Value = "desc")]
        Descending,

        /// <summary>
        /// Ascending sort order
        /// </summary>
        [EnumMember(Value = "asc")]
        Ascending,
    }
}
