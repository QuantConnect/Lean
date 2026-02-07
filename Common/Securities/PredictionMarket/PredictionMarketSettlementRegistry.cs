/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using System;
using System.Collections.Concurrent;

namespace QuantConnect.Securities.PredictionMarket
{
    /// <summary>
    /// Static thread-safe registry for prediction market settlement data.
    /// Maps symbols to their close times and settlement results.
    /// </summary>
    /// <remarks>
    /// This registry is needed because:
    /// - SecurityIdentifier.GeneratePredictionMarket uses DefaultDate (no date in SID)
    /// - The close time comes from the Kalshi API at runtime, not from map files
    /// - Populated by KalshiUniverse during symbol selection
    /// - Read by the delisting event provider and portfolio model
    /// </remarks>
    public static class PredictionMarketSettlementRegistry
    {
        private static readonly ConcurrentDictionary<Symbol, DateTime> _delistingDates = new();
        private static readonly ConcurrentDictionary<Symbol, PredictionMarketSettlementResult> _results = new();

        /// <summary>
        /// Registers the delisting date (close time) for a prediction market symbol
        /// </summary>
        /// <param name="symbol">The prediction market symbol</param>
        /// <param name="delistingDate">The date when the market closes/delists</param>
        public static void SetDelistingDate(Symbol symbol, DateTime delistingDate)
        {
            _delistingDates[symbol] = delistingDate;
        }

        /// <summary>
        /// Attempts to get the delisting date for a prediction market symbol
        /// </summary>
        /// <param name="symbol">The prediction market symbol</param>
        /// <param name="delistingDate">The delisting date if found</param>
        /// <returns>True if the delisting date was found, false otherwise</returns>
        public static bool TryGetDelistingDate(Symbol symbol, out DateTime delistingDate)
        {
            return _delistingDates.TryGetValue(symbol, out delistingDate);
        }

        /// <summary>
        /// Registers the settlement result for a prediction market symbol
        /// </summary>
        /// <param name="symbol">The prediction market symbol</param>
        /// <param name="result">The settlement result (Yes/No)</param>
        public static void SetResult(Symbol symbol, PredictionMarketSettlementResult result)
        {
            _results[symbol] = result;
        }

        /// <summary>
        /// Attempts to get the settlement result for a prediction market symbol
        /// </summary>
        /// <param name="symbol">The prediction market symbol</param>
        /// <param name="result">The settlement result if found</param>
        /// <returns>True if the result was found, false otherwise</returns>
        public static bool TryGetResult(Symbol symbol, out PredictionMarketSettlementResult result)
        {
            return _results.TryGetValue(symbol, out result);
        }

        /// <summary>
        /// Gets the settlement result for a symbol, returning Pending if not found
        /// </summary>
        /// <param name="symbol">The prediction market symbol</param>
        /// <returns>The settlement result, or Pending if not registered</returns>
        public static PredictionMarketSettlementResult GetResult(Symbol symbol)
        {
            return _results.TryGetValue(symbol, out var result) ? result : PredictionMarketSettlementResult.Pending;
        }

        /// <summary>
        /// Clears all registered data. Primarily for testing purposes.
        /// </summary>
        public static void Clear()
        {
            _delistingDates.Clear();
            _results.Clear();
        }

        /// <summary>
        /// Removes a specific symbol from the registry
        /// </summary>
        /// <param name="symbol">The symbol to remove</param>
        public static void Remove(Symbol symbol)
        {
            _delistingDates.TryRemove(symbol, out _);
            _results.TryRemove(symbol, out _);
        }
    }
}
