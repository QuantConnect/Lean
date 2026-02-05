/*
 * Cascade Labs - Hyperliquid Algorithm Extensions
 * Extension methods for adding Hyperliquid universes to algorithms
 */

using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Extension methods for adding Hyperliquid universes to algorithms
    /// </summary>
    public static class HyperliquidAlgorithmExtensions
    {
        /// <summary>
        /// Add universe of all Hyperliquid contracts (perps + spot)
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddHyperliquidUniverse(
            this QCAlgorithm algorithm,
            TimeSpan? refreshInterval = null)
        {
            var universe = new HyperliquidUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Hyperliquid contracts filtered by security type
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="securityTypes">Security types to include (CryptoFuture for perps, Crypto for spot)</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddHyperliquidUniverse(
            this QCAlgorithm algorithm,
            SecurityType[] securityTypes,
            TimeSpan? refreshInterval = null)
        {
            var universe = new HyperliquidUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                securityTypeFilter: securityTypes);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe with custom filter using HyperliquidUniverseData
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="selector">Predicate to filter contracts</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddHyperliquidUniverse(
            this QCAlgorithm algorithm,
            Func<HyperliquidUniverseData, bool> selector,
            TimeSpan? refreshInterval = null)
        {
            var universe = new HyperliquidUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                selector: selector);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe with custom filter using HyperliquidUniverseData (Python-compatible)
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="selector">Python predicate to filter contracts</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddHyperliquidUniverse(
            this QCAlgorithm algorithm,
            PyObject selector,
            TimeSpan? refreshInterval = null)
        {
            Func<HyperliquidUniverseData, bool>? filter = null;

            if (selector.TrySafeAs(out Func<HyperliquidUniverseData, object> pyFunc))
            {
                filter = data =>
                {
                    var result = pyFunc(data);
                    return result is bool b ? b : Convert.ToBoolean(result);
                };
            }

            var universe = new HyperliquidUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                selector: filter);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Hyperliquid contracts filtered by security type with custom selector
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="securityTypes">Security types to include</param>
        /// <param name="selector">Predicate to filter contracts</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddHyperliquidUniverse(
            this QCAlgorithm algorithm,
            SecurityType[] securityTypes,
            Func<HyperliquidUniverseData, bool> selector,
            TimeSpan? refreshInterval = null)
        {
            var universe = new HyperliquidUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                securityTypeFilter: securityTypes,
                selector: selector);
            return algorithm.AddUniverse(universe);
        }

        /// <summary>
        /// Add universe of Hyperliquid contracts filtered by security type with Python selector
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="securityTypes">Security types to include</param>
        /// <param name="selector">Python predicate to filter contracts</param>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <returns>The created universe</returns>
        public static Universe AddHyperliquidUniverse(
            this QCAlgorithm algorithm,
            SecurityType[] securityTypes,
            PyObject selector,
            TimeSpan? refreshInterval = null)
        {
            Func<HyperliquidUniverseData, bool>? filter = null;

            if (selector.TrySafeAs(out Func<HyperliquidUniverseData, object> pyFunc))
            {
                filter = data =>
                {
                    var result = pyFunc(data);
                    return result is bool b ? b : Convert.ToBoolean(result);
                };
            }

            var universe = new HyperliquidUniverse(
                algorithm.UniverseSettings,
                refreshInterval ?? Time.OneDay,
                securityTypeFilter: securityTypes,
                selector: filter);
            return algorithm.AddUniverse(universe);
        }
    }
}
