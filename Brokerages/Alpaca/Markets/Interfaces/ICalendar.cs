/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates single trading day information from Alpaca REST API.
    /// </summary>
    public interface ICalendar
    {
        /// <summary>
        /// Gets trading date (in UTC time zone).
        /// </summary>
        DateTime TradingDate { get; }

        /// <summary>
        /// Gets trading date open time (in UTC time zone).
        /// </summary>
        DateTime TradingOpenTime { get; }

        /// <summary>
        /// Gets trading date close time (in UTC time zone).
        /// </summary>
        DateTime TradingCloseTime { get; }
    }
}