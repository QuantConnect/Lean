/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates account configuration settings from Alpaca REST API.
    /// </summary>
    public interface IAccountConfiguration
    {
        /// <summary>
        /// Gets or sets day trade margin call protection mode for account.
        /// </summary>
        DayTradeMarginCallProtection DayTradeMarginCallProtection { get; set; }

        /// <summary>
        /// Gets or sets notification level for order fill emails.
        /// </summary>
        TradeConfirmEmail TradeConfirmEmail { get; set; }

        /// <summary>
        /// Gets or sets control flag for blocking new orders placement.
        /// </summary>
        Boolean IsSuspendTrade { get; set; }

        /// <summary>
        /// Gets or sets control flag for enabling long-only account mode.
        /// </summary>
        Boolean IsNoShorting { get; set; }
    }
}
