/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates account update information from Alpaca streaming API.
    /// </summary>
    public interface IAccountUpdate : IAccountBase
    {
        /// <summary>
        /// Gets timestamp of last account update event.
        /// </summary>
        DateTime UpdatedAt { get; }

        /// <summary>
        /// Gets timestamp of account deletion event.
        /// </summary>
        DateTime? DeletedAt { get; }
    }
}
