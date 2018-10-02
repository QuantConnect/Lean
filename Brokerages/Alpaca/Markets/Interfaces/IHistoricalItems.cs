/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
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
        /// Gets read-only collection of histrical items.
        /// </summary>
        IReadOnlyCollection<TItem> Items { get; }
    }
}