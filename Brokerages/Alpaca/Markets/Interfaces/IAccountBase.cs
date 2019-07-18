/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates basic account information from Alpaca streaming API.
    /// </summary>
    public interface IAccountBase
    {
        /// <summary>
        /// Gets unique account identifier.
        /// </summary>
        Guid AccountId { get; }

        /// <summary>
        /// Gets updated account status.
        /// </summary>
        AccountStatus Status { get; }

        /// <summary>
        /// Gets main account currency.
        /// </summary>
        String Currency { get; }

        /// <summary>
        /// Gets amount of money available for trading.
        /// </summary>
        Decimal TradableCash { get; }

        /// <summary>
        /// Gets amount of money available for withdraw.
        /// </summary>
        Decimal WithdrawableCash { get; }

        /// <summary>
        /// Gets timestamp of account creation event.
        /// </summary>
        DateTime CreatedAt { get; }
    }
}
