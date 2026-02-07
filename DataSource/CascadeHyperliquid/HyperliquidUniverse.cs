/*
 * Cascade Labs - Hyperliquid Universe
 * Universe implementation for Hyperliquid perpetual futures and spot contracts
 */

using Newtonsoft.Json.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Logging;
using QuantConnect.Scheduling;

namespace QuantConnect.Lean.DataSource.CascadeHyperliquid
{
    /// <summary>
    /// Universe implementation for Hyperliquid perpetual futures and spot contracts.
    /// Fetches active contracts from the Hyperliquid API and converts to LEAN symbols.
    /// Uses ScheduledUniverse to trigger at specific times.
    /// </summary>
    public class HyperliquidUniverse : ScheduledUniverse
    {
        private readonly SecurityType[]? _securityTypeFilter;
        private readonly Func<HyperliquidUniverseData, bool>? _selector;
        private HyperliquidRestClient? _restClient;
        private HyperliquidSymbolMapper? _symbolMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperliquidUniverse"/> class
        /// </summary>
        /// <param name="universeSettings">Universe settings for subscriptions</param>
        /// <param name="refreshInterval">How often to refresh the universe</param>
        /// <param name="securityTypeFilter">Filter to perps only, spot only, or both (default: both)</param>
        /// <param name="selector">Custom predicate filter</param>
        public HyperliquidUniverse(
            UniverseSettings universeSettings,
            TimeSpan refreshInterval,
            SecurityType[]? securityTypeFilter = null,
            Func<HyperliquidUniverseData, bool>? selector = null)
            : base(
                TimeZones.Utc,
                CreateDateRule(),
                CreateTimeRule(),
                dt => Enumerable.Empty<Symbol>(),
                universeSettings)
        {
            _securityTypeFilter = securityTypeFilter;
            _selector = selector;
        }

        private static IDateRule CreateDateRule()
        {
            return new FuncDateRule("EveryDay", (start, end) =>
            {
                var dates = new List<DateTime>();
                var current = start.Date;
                while (current <= end.Date)
                {
                    dates.Add(current);
                    current = current.AddDays(1);
                }
                return dates;
            });
        }

        private static ITimeRule CreateTimeRule()
        {
            return new FuncTimeRule("MidnightUTC", dates =>
                dates.Select(d => d.Date));
        }

        /// <summary>
        /// Performs universe selection by fetching contracts from API
        /// </summary>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return FetchActiveContracts(utcTime);
        }

        private List<Symbol> FetchActiveContracts(DateTime utcTime)
        {
            _restClient ??= new HyperliquidRestClient();
            _symbolMapper ??= new HyperliquidSymbolMapper();

            var symbols = new List<Symbol>();
            var includePerps = _securityTypeFilter == null || _securityTypeFilter.Contains(SecurityType.CryptoFuture);
            var includeSpot = _securityTypeFilter == null || _securityTypeFilter.Contains(SecurityType.Crypto);

            try
            {
                if (includePerps)
                {
                    FetchPerps(utcTime, symbols);
                }

                if (includeSpot)
                {
                    FetchSpot(utcTime, symbols);
                }

                Log.Trace($"HyperliquidUniverse: Selected {symbols.Count} contracts at {utcTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Log.Error($"HyperliquidUniverse: Error fetching contracts: {ex.Message}");
            }

            return symbols;
        }

        private void FetchPerps(DateTime utcTime, List<Symbol> symbols)
        {
            var response = _restClient!.GetMetaAndAssetCtxsAsync().SynchronouslyAwaitTaskResult();
            if (response == null || response.Count < 2)
            {
                Log.Error("HyperliquidUniverse: Failed to fetch perp metadata");
                return;
            }

            var meta = response[0] as JObject;
            var assetCtxs = response[1] as JArray;

            if (meta == null || assetCtxs == null)
            {
                Log.Error("HyperliquidUniverse: Invalid perp metadata format");
                return;
            }

            var universe = meta["universe"] as JArray;
            if (universe == null)
            {
                Log.Error("HyperliquidUniverse: No perp universe array in metadata");
                return;
            }

            for (var i = 0; i < universe.Count && i < assetCtxs.Count; i++)
            {
                try
                {
                    var metaEntry = universe[i];
                    var assetCtx = assetCtxs[i];
                    var coin = metaEntry["name"]?.Value<string>();

                    if (string.IsNullOrEmpty(coin))
                    {
                        continue;
                    }

                    var symbol = _symbolMapper!.GetLeanSymbol(coin, SecurityType.CryptoFuture, QuantConnect.Market.Hyperliquid);

                    if (_selector != null)
                    {
                        var universeData = HyperliquidUniverseData.FromApiData(
                            coin, SecurityType.CryptoFuture, metaEntry, assetCtx, symbol, utcTime);

                        if (!_selector(universeData))
                        {
                            continue;
                        }
                    }

                    symbols.Add(symbol);
                }
                catch (Exception ex)
                {
                    Log.Error($"HyperliquidUniverse: Error processing perp contract at index {i}: {ex.Message}");
                }
            }

            Log.Trace($"HyperliquidUniverse: Fetched {symbols.Count} perp contracts");
        }

        private void FetchSpot(DateTime utcTime, List<Symbol> symbols)
        {
            var response = _restClient!.GetSpotMetaAndAssetCtxsAsync().SynchronouslyAwaitTaskResult();
            if (response == null || response.Count < 2)
            {
                Log.Error("HyperliquidUniverse: Failed to fetch spot metadata");
                return;
            }

            var meta = response[0] as JObject;
            var assetCtxs = response[1] as JArray;

            if (meta == null || assetCtxs == null)
            {
                Log.Error("HyperliquidUniverse: Invalid spot metadata format");
                return;
            }

            var tokens = meta["tokens"] as JArray;
            if (tokens == null)
            {
                Log.Error("HyperliquidUniverse: No tokens array in spot metadata");
                return;
            }

            var spotCount = 0;

            for (var i = 0; i < tokens.Count && i < assetCtxs.Count; i++)
            {
                try
                {
                    var metaEntry = tokens[i];
                    var assetCtx = assetCtxs[i];
                    var coin = metaEntry["name"]?.Value<string>();

                    if (string.IsNullOrEmpty(coin))
                    {
                        continue;
                    }

                    var symbol = _symbolMapper!.GetLeanSymbol(coin, SecurityType.Crypto, QuantConnect.Market.Hyperliquid);

                    if (_selector != null)
                    {
                        var universeData = HyperliquidUniverseData.FromApiData(
                            coin, SecurityType.Crypto, metaEntry, assetCtx, symbol, utcTime);

                        if (!_selector(universeData))
                        {
                            continue;
                        }
                    }

                    symbols.Add(symbol);
                    spotCount++;
                }
                catch (Exception ex)
                {
                    Log.Error($"HyperliquidUniverse: Error processing spot contract at index {i}: {ex.Message}");
                }
            }

            Log.Trace($"HyperliquidUniverse: Fetched {spotCount} spot contracts");
        }
    }
}
