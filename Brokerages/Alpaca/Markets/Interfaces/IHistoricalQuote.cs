/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates historical quote information from Polygon REST API.
    /// </summary>
    public interface IHistoricalQuote
    {
        /// <summary>
        /// Gets bid source exchange.
        /// </summary>
        String BidExchange { get; }

        /// <summary>
        /// Gets ask source exchange.
        /// </summary>
        String AskExchange { get; }

        /// <summary>
        /// Gets time offset of quote.
        /// </summary>
        Int64 TimeOffset { get; }

        /// <summary>
        /// Gets bid price level.
        /// </summary>
        Decimal BidPrice { get; }

        /// <summary>
        /// Gets ask price level.
        /// </summary>
        Decimal AskPrice { get; }

        /// <summary>
        /// Gets bid quantity.
        /// </summary>
        Int64 BidSize { get; }

        /// <summary>
        /// Gets ask quantity.
        /// </summary>
        Int64 AskSize { get; }
    }
}