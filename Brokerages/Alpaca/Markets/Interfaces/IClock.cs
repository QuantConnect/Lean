/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates current trading date information from Alpaca REST API.
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Gets current timestamp (in UTC time zone).
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Returns <c>true</c> if trading day is open now.
        /// </summary>
        Boolean IsOpen { get; }

        /// <summary>
        /// Gets nearest trading day open time (in UTC time zone).
        /// </summary>
        DateTime NextOpen { get; }

        /// <summary>
        /// Gets nearest trading day close time (in UTC time zone).
        /// </summary>
        DateTime NextClose { get;  }
    }
}
