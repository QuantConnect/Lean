/*
 * Cascade Labs - Kalshi Algorithm Extensions
 * Extension methods for adding Kalshi universes to algorithms
 */

using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Extension methods for adding Kalshi universes to algorithms
    /// </summary>
    public static class KalshiAlgorithmExtensions
    {
        /// <summary>
        /// Add universe of all open Kalshi prediction markets
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            TimeSpan? refreshInterval = null)
        {
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Kalshi markets filtered by series
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="seriesTickers">Series to include (e.g., "KXHIGHNY", "INXD")</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            params string[] seriesTickers)
        {
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                Time.OneDay,
                seriesFilter: seriesTickers);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Kalshi markets filtered by category
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="categories">Categories to include</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            params KalshiCategory[] categories)
        {
            var categoryStrings = categories.Select(c => c.ToString()).ToArray();
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                Time.OneDay,
                categoryFilter: categoryStrings);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe with custom market filter using KalshiUniverseData
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="selector">Predicate to filter markets</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            Func<KalshiUniverseData, bool> selector,
            TimeSpan? refreshInterval = null)
        {
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                universeDataFilter: selector);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe with custom market filter using raw KalshiMarket
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="selector">Predicate to filter markets</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverseRaw(
            this QCAlgorithm algorithm,
            Func<KalshiMarket, bool> selector,
            TimeSpan? refreshInterval = null)
        {
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                marketFilter: selector);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Kalshi markets filtered by series with custom refresh interval
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="seriesTickers">Series to include</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            string[] seriesTickers,
            TimeSpan refreshInterval)
        {
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval,
                seriesFilter: seriesTickers);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Kalshi markets filtered by series with custom selector
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="seriesTickers">Series to include (filters at API level for efficiency)</param>
        /// <param name="selector">Additional predicate to filter markets</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            string[] seriesTickers,
            Func<KalshiUniverseData, bool> selector,
            TimeSpan? refreshInterval = null)
        {
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                seriesFilter: seriesTickers,
                universeDataFilter: selector);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Kalshi markets filtered by series with custom selector (Python)
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="seriesTickers">Series to include (filters at API level for efficiency)</param>
        /// <param name="selector">Python predicate to filter markets</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            string[] seriesTickers,
            PyObject selector,
            TimeSpan? refreshInterval = null)
        {
            Func<KalshiUniverseData, bool>? filter = null;

            if (selector.TrySafeAs(out Func<KalshiUniverseData, object> pyFunc))
            {
                // Wrap Python function that returns object to bool
                filter = data =>
                {
                    var result = pyFunc(data);
                    return result is bool b ? b : Convert.ToBoolean(result);
                };
            }

            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                seriesFilter: seriesTickers,
                universeDataFilter: filter);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Kalshi markets filtered by category with custom refresh interval
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="categories">Categories to include</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <returns>The created universe</returns>
        public static Universe AddKalshiUniverse(
            this QCAlgorithm algorithm,
            KalshiCategory[] categories,
            TimeSpan refreshInterval)
        {
            var categoryStrings = categories.Select(c => c.ToString()).ToArray();
            var universe = new KalshiUniverse(
                algorithm.UniverseSettings,
                refreshInterval,
                categoryFilter: categoryStrings);
            return algorithm.AddUniverse(universe);
        }
    }
}
