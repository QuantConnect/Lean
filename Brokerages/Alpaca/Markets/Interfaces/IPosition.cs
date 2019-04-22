/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates position information from Alpaca REST API.
    /// </summary>
    public interface IPosition
    {
        /// <summary>
        /// Gets unique account identifier.
        /// </summary>
        Guid AccountId { get; }

        /// <summary>
        /// Gets unique asset identifier.
        /// </summary>
        Guid AssetId { get; }

        /// <summary>
        /// Gets asset name.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Gets asset exchange.
        /// </summary>
        Exchange Exchange { get; }

        /// <summary>
        /// Gets asset class.
        /// </summary>
        AssetClass AssetClass { get; }

        /// <summary>
        /// Gets average entry price for position.
        /// </summary>
        Decimal AverageEntryPrice { get; }

        /// <summary>
        /// Get position quantity (size).
        /// </summary>
        Int32 Quantity { get; }

        /// <summary>
        /// Get position side (short or long).
        /// </summary>
        PositionSide Side { get; }

        /// <summary>
        /// Get current position market value.
        /// </summary>
        Decimal MarketValue { get; }

        /// <summary>
        /// Get postion cost basis.
        /// </summary>
        Decimal CostBasis { get; }

        /// <summary>
        /// Get position unrealized profit loss.
        /// </summary>
        Decimal UnrealizedProfitLoss { get; }

        /// <summary>
        /// Get position unrealized profit loss in percent.
        /// </summary>
        Decimal UnrealizedProfitLossPercent { get; }

        /// <summary>
        /// Get position intraday unrealized profit loss.
        /// </summary>
        Decimal IntradayUnrealizedProfitLoss { get; }

        /// <summary>
        /// Get position intraday unrealized profit loss in percent.
        /// </summary>
        Decimal IntradayUnrealizedProfitLossPercent { get; }

        /// <summary>
        /// Gets position's asset current price.
        /// </summary>
        Decimal AssetCurrentPrice { get; }

        /// <summary>
        /// Gets position's asset last trade price.
        /// </summary>
        Decimal AssetLastPrice { get; }

        /// <summary>
        /// Gets position's asset price change in percent.
        /// </summary>
        Decimal AssetChangePercent { get; }
    }
}
