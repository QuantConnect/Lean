/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates position action status information from Alpaca REST API.
    /// </summary>
    public interface IPositionActionStatus
    {
        /// <summary>
        /// Gets processed position asset name.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Returns <c>true</c> if requested action completed successfully.
        /// </summary>
        Boolean IsSuccess { get; }
    }
}
