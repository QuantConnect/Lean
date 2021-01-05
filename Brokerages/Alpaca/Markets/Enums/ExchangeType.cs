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
    /// Supported exchange types in Polygon REST API.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ExchangeType
    {
        /// <summary>
        /// Ordinal exchange.
        /// </summary>
        [EnumMember(Value = "exchange")]
        Exchange,

        /// <summary>
        /// Banking organization.
        /// </summary>
        [EnumMember(Value = "banking")]
        Banking,

        /// <summary>
        /// Trade reporting facility.
        /// </summary>
        [EnumMember(Value = "TRF")]
        TradeReportingFacility
    }
}
