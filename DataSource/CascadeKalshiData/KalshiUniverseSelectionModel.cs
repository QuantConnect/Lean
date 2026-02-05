/*
 * Cascade Labs - Kalshi Universe Selection Model
 * Universe selection model for Kalshi prediction markets
 */

using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Universe selection model for Kalshi prediction markets.
    /// Provides daily universe of active contracts with optional series filtering.
    /// </summary>
    public class KalshiUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly string[]? _seriesFilter;
        private readonly string[]? _categoryFilter;
        private readonly Func<KalshiMarket, bool>? _marketFilter;
        private readonly Func<KalshiUniverseData, bool>? _universeDataFilter;
        private readonly TimeSpan _refreshInterval;
        private readonly UniverseSettings? _universeSettings;

        /// <summary>
        /// Create universe of all open Kalshi markets
        /// </summary>
        public KalshiUniverseSelectionModel(
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Create universe filtered by series tickers
        /// </summary>
        /// <param name="seriesFilter">Series to include (e.g., "KXHIGHNY", "INXD")</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public KalshiUniverseSelectionModel(
            string[] seriesFilter,
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _seriesFilter = seriesFilter;
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Create universe filtered by category
        /// </summary>
        /// <param name="categoryFilter">Categories to include (e.g., Weather, Finance)</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public KalshiUniverseSelectionModel(
            KalshiCategory[] categoryFilter,
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _categoryFilter = categoryFilter.Select(c => c.ToString()).ToArray();
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Create universe with custom market filter using KalshiMarket
        /// </summary>
        /// <param name="marketFilter">Predicate to filter markets</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public KalshiUniverseSelectionModel(
            Func<KalshiMarket, bool> marketFilter,
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _marketFilter = marketFilter;
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Create universe with custom filter using KalshiUniverseData
        /// </summary>
        /// <param name="universeDataFilter">Predicate to filter markets using KalshiUniverseData</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public KalshiUniverseSelectionModel(
            Func<KalshiUniverseData, bool> universeDataFilter,
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _universeDataFilter = universeDataFilter;
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Creates the Kalshi universe
        /// </summary>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            var universeSettings = _universeSettings ?? algorithm.UniverseSettings;

            yield return new KalshiUniverse(
                universeSettings,
                _refreshInterval,
                _seriesFilter,
                _categoryFilter,
                _marketFilter,
                _universeDataFilter);
        }
    }
}
