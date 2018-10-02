/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates historical trade infromation from Polygon REST API.
    /// </summary>
    public interface IHistoricalTrade
    {
        /// <summary>
        /// Gets trade source exchange.
        /// </summary>
        String Exchange { get; }

        /// <summary>
        /// Gets trade timestamp.
        /// </summary>
        Int64 TimeOffset  { get; }

        /// <summary>
        /// Gets trade price.
        /// </summary>
        Decimal Price { get; }

        /// <summary>
        /// Gets trade quantity.
        /// </summary>
        Int64 Size { get; }
    }
}