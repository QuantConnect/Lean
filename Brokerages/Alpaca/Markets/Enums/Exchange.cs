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
    /// Exchanges supported by Alpaca REST API.
    /// </summary>
    [JsonConverter(typeof(ExchangeEnumConverter))]
    public enum Exchange
    {
        /// <summary>
        /// Unknown exchange (not supported by this version of SDK).
        /// </summary>
        [EnumMember(Value = "UNKNOWN")]
        Unknown,

        /// <summary>
        /// NYSE American Stock Exchange.
        /// </summary>
        [EnumMember(Value = "NYSEMKT")]
        NyseMkt,

        /// <summary>
        /// NYSE Arca Stock Exchange.
        /// </summary>
        [EnumMember(Value = "NYSEARCA")]
        NyseArca,

        /// <summary>
        /// New York Stock Exchange (NYSE)
        /// </summary>
        [EnumMember(Value = "NYSE")]
        Nyse,

        /// <summary>
        /// Nasdaq Stock Market.
        /// </summary>
        [EnumMember(Value = "NASDAQ")]
        Nasdaq,

        /// <summary>
        /// BATS Global Market.
        /// </summary>
        [EnumMember(Value = "BATS")]
        Bats,

        /// <summary>
        /// American Stock Exchange (AMEX)
        /// </summary>
        [EnumMember(Value = "AMEX")]
        Amex,

        /// <summary>
        /// Archipelago Stock Exchange (ARCA).
        /// </summary>
        [EnumMember(Value = "ARCA")]
        Arca,

        /// <summary>
        /// International Exchange (IEX).
        /// </summary>
        [EnumMember(Value = "IEX")]
        Iex
    }
}