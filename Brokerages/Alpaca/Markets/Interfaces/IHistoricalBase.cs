/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates base historical information from Polygon REST API.
    /// </summary>
    public interface IHistoricalBase
    {
        /// <summary>
        /// Gets tape where trade occured.
        /// </summary>
        Int64 Tape { get; }

        /// <summary>
        /// Gets sequence number of trade.
        /// </summary>
        Int64 SequenceNumber { get; }

        /// <summary>
        /// Gets quote conditions.
        /// </summary>
        IReadOnlyList<Int64> Conditions { get; }
    }
}