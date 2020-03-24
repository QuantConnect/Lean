/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates read-only access for historical items in Polygon REST API.
    /// </summary>
    /// <typeparam name="TItem">Type of historical items inside this container.</typeparam>
    public interface IHistoricalItems<out TItem>
    {
        /// <summary>
        /// Gets resulting status of historical data request.
        /// </summary>
        String Status { get; }

        /// <summary>
        /// Gets asset name for all historical items in container.
        /// </summary>
        String Symbol { get; }

        /// <summary>
        /// Gets read-only collection of historical items.
        /// </summary>
        IReadOnlyList<TItem> Items { get; }

        /// <summary>
        /// Indicates if this response was adjusted for splits.
        /// Polygon v2 API only.
        /// </summary>
        Boolean Adjusted { get; }

        /// <summary>
        /// Number of aggregates (minutes or days) used to generate the response.
        /// Polygon v2 API only.
        /// </summary>
        Int64 QueryCount { get; }

        /// <summary>
        /// Total number of results generated.
        /// Polygon v2 API only.
        /// </summary>
        Int64 ResultsCount { get; }

        /// <summary>
        /// Query execution database latency reported by Polygon.
        /// </summary>
        [Obsolete("This property will be removed in upcoming major version of SDK.", true)]
        TimeSpan DatabaseLatency { get; }
    }
}
