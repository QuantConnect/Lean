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