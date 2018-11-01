/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/commit/161b114b4b40d852a14a903bd6e69d26fe637922
*/

using System;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Encapsulates list of single day historical itmes from Polygon REST API.
    /// </summary>
    /// <typeparam name="TItem">Type of historical items inside this container.</typeparam>
    public interface IDayHistoricalItems<out TItem> : IHistoricalItems<TItem>
    {
        /// <summary>
        /// Gets historical items day.
        /// </summary>
        DateTime ItemsDay { get; }

        /// <summary>
        /// Gets data latency in milliseconds.
        /// </summary>
        Int64 LatencyInMs { get; }
    }
}