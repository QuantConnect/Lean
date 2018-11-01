/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates account information from Alpaca REST API.
    /// </summary>
    public interface IAccount
    {
        /// <summary>
        /// Gets unique account identifier.
        /// </summary>
        Guid AccountId { get; }

        /// <summary>
        /// Gets current account status.
        /// </summary>
        AccountStatus Status { get; }

        /// <summary>
        /// Gets main account currency.
        /// </summary>
        String Currency { get; }

        /// <summary>
        /// Gets amount of money avaliable for trading.
        /// </summary>
        Decimal TradableCash { get;  }

        /// <summary>
        /// Gets amount of money avaliable for withdraw.
        /// </summary>
        Decimal WithdrawableCash { get;  }

        /// <summary>
        /// Gets total account portfolio value.
        /// </summary>
        Decimal PortfolioValue { get;  }

        /// <summary>
        /// Gets returns <c>true</c> if account is linked to day pattern trader.
        /// </summary>
        Boolean IsDayPatternTrader { get;  }

        /// <summary>
        /// Gets returns <c>true</c> if account trading function sare blocked.
        /// </summary>
        Boolean IsTradingBlocked { get; }

        /// <summary>
        /// Gets returns <c>true</c> if account transfer functions are blocked.
        /// </summary>
        Boolean IsTransfersBlocked { get; }

        /// <summary>
        /// Gets returns <c>true</c> if account is completely blocked.
        /// </summary>
        Boolean IsAccountBlocked { get; }

        /// <summary>
        /// Gets timestamp of account creation event.
        /// </summary>
        DateTime CreatedAt { get; }
    }
}