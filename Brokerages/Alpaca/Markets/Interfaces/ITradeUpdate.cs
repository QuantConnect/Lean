/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates trade update information from Alpaca streaming API.
    /// </summary>
    public interface ITradeUpdate
    {
        /// <summary>
        /// Gets trade update reason.
        /// </summary>
        TradeEvent Event { get; }

        /// <summary>
        /// Gets updated trade price level.
        /// </summary>
        Decimal? Price { get; }

        /// <summary>
        /// Gets updated trade quantity.
        /// </summary>
        Int64? Quantity { get; }

        /// <summary>
        /// Gets update timestamp.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets related order object.
        /// </summary>
        IOrder Order { get; }
    }
}
