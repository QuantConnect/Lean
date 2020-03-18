/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Changes from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates historical quote information from Polygon REST API.
    /// </summary>
    // ReSharper disable once PossibleInterfaceMemberAmbiguity
    public interface IHistoricalQuote : IQuoteBase<String>, IQuoteBase<Int64>, ITimestamps, IHistoricalBase
    {
        /// <summary>
        /// Gets time offset of quote.
        /// </summary>
        [Obsolete("TimeOffset is deprecated in API v2, use Timestamp instead", true)]
        Int64 TimeOffset { get; }

        /// <summary>
        /// Gets indicators.
        /// </summary>
        IReadOnlyList<Int64> Indicators { get; }
    }
}
