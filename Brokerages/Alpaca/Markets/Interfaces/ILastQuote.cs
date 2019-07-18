/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates last quote information from Alpaca REST API.
    /// </summary>
    public interface ILastQuote : IStreamQuote
    {
        /// <summary>
        /// Gets quote response status.
        /// </summary>
        String Status { get; }
    }
}
