/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates quote information from Polygon streaming API.
    /// </summary>
    public interface IStreamQuote : IQuoteBase<Int64>
    {
        /// <summary>
        /// Gets asset name.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Gets quote timestamp.
        /// </summary>
        DateTime Time { get; }
    }
}
