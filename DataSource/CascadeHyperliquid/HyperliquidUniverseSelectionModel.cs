/*
 * Cascade Labs - Hyperliquid Universe Selection Model
 * Universe selection model for Hyperliquid perpetual futures and spot contracts
 */

using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Universe selection model for Hyperliquid perpetual futures and spot contracts.
    /// Provides daily universe of active contracts with optional filtering.
    /// </summary>
    public class HyperliquidUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly SecurityType[]? _securityTypeFilter;
        private readonly Func<HyperliquidUniverseData, bool>? _selector;
        private readonly TimeSpan _refreshInterval;
        private readonly UniverseSettings? _universeSettings;

        /// <summary>
        /// Create universe of all Hyperliquid contracts (perps + spot)
        /// </summary>
        /// <param name="refreshInterval">How often to refresh the universe (default: daily)</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public HyperliquidUniverseSelectionModel(
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Create universe filtered by security type
        /// </summary>
        /// <param name="securityTypeFilter">Security types to include (CryptoFuture, Crypto)</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public HyperliquidUniverseSelectionModel(
            SecurityType[] securityTypeFilter,
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _securityTypeFilter = securityTypeFilter;
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Create universe with custom filter using HyperliquidUniverseData
        /// </summary>
        /// <param name="selector">Predicate to filter contracts</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        public HyperliquidUniverseSelectionModel(
            Func<HyperliquidUniverseData, bool> selector,
            TimeSpan? refreshInterval = null,
            UniverseSettings? universeSettings = null)
        {
            _selector = selector;
            _refreshInterval = refreshInterval ?? Time.OneDay;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Creates the Hyperliquid universe
        /// </summary>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            var universeSettings = _universeSettings ?? algorithm.UniverseSettings;

            yield return new HyperliquidUniverse(
                universeSettings,
                _refreshInterval,
                _securityTypeFilter,
                _selector);
        }
    }
}
