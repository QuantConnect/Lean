/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Updates from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates historical trade information from Polygon REST API.
    /// </summary>
    public interface IHistoricalTrade : ITimestamps, IHistoricalBase
    {
        /// <summary>
        /// Gets trade source exchange.
        /// </summary>
        [Obsolete("Exchange is deprecated in API v2, use ExchangeId instead", true)]
        String Exchange { get; }

        /// <summary>
        /// Gets trade source exchange identifier.
        /// </summary>
        Int64 ExchangeId { get; }

        /// <summary>
        /// Gets trade timestamp.
        /// </summary>
        [Obsolete("TimeOffset is deprecated in API v2, use Timestamp instead", true)]
        Int64 TimeOffset  { get; }

        /// <summary>
        /// Gets trade price.
        /// </summary>
        Decimal Price { get; }

        /// <summary>
        /// Gets trade quantity.
        /// </summary>
        Int64 Size { get; }

        /// <summary>
        /// Gets trade reporting facility ID.
        /// </summary>
        Int64 TradeReportingFacilityId { get; }

        /// <summary>
        /// Gets trade ID.
        /// </summary>
        String TradeId { get; }

        /// <summary>
        /// Gets original trade ID.
        /// </summary>
        String OriginalTradeId { get; }
    }
}