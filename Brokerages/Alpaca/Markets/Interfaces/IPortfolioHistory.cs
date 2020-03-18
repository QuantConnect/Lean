/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates portfolio history information from Alpaca REST API.
    /// </summary>
    public interface IPortfolioHistory
    {
        /// <summary>
        /// Gets historical information items list with timestamps.
        /// </summary>
        IReadOnlyList<IPortfolioHistoryItem> Items { get; }

        /// <summary>
        /// Gets time frame value for this historical view.
        /// </summary>
        TimeFrame TimeFrame { get; }

        /// <summary>
        /// Gets base value for this historical view.
        /// </summary>
        Decimal BaseValue { get; }
    }
}
