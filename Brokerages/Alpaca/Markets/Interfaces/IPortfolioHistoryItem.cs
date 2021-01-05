/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates portfolio history information item from Alpaca REST API.
    /// </summary>
    public interface IPortfolioHistoryItem
    {
        /// <summary>
        /// Gets historical equity value.
        /// </summary>
        Decimal? Equity { get; }

        /// <summary>
        /// Gets historical profit/loss value.
        /// </summary>
        Decimal? ProfitLoss { get; }

        /// <summary>
        /// Gets historical profit/loss value as percentages.
        /// </summary>
        Decimal? ProfitLossPercentage { get; }

        /// <summary>
        /// Gets historical timestamp value.
        /// </summary>
        DateTime Timestamp { get; }
    }
}