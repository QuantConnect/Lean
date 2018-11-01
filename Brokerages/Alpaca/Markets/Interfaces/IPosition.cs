/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
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
        /// Gets unique account indentifier.
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
        /// Get poristion side (short or long).
        /// </summary>
        PositionSide Side { get; }

        /// <summary>
        /// Get current postion market value.
        /// </summary>
        Decimal MarketValue { get; }

        /// <summary>
        /// Get postion cost basis.
        /// </summary>
        Decimal CostBasis { get; }

        /// <summary>
        /// Get postion unrealized profit loss.
        /// </summary>
        Decimal UnrealizedProfitLoss { get; }

        /// <summary>
        /// Get postion unrealized profit loss in percent.
        /// </summary>
        Decimal UnrealizedProfitLossPercent { get; }

        /// <summary>
        /// Get postion intraday unrealized profit loss.
        /// </summary>
        Decimal IntadayUnrealizedProfitLoss { get; }

        /// <summary>
        /// Get postion intraday unrealized profit loss in percent.
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