/*
 * Cascade Labs - Kalshi Universe
 * Universe implementation for Kalshi prediction markets
 */

using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Logging;
using QuantConnect.Scheduling;
using QuantConnect.Securities.PredictionMarket;
using QuantConnect.Lean.DataSource.CascadeKalshiData.Models;

namespace QuantConnect.Lean.DataSource.CascadeKalshiData
{
    /// <summary>
    /// Universe implementation for Kalshi prediction markets.
    /// Fetches active markets from Kalshi API and converts to LEAN symbols.
    /// Uses ScheduledUniverse to trigger at specific times.
    /// </summary>
    public class KalshiUniverse : ScheduledUniverse
    {
        private readonly string[]? _seriesFilter;
        private readonly string[]? _categoryFilter;
        private readonly Func<KalshiMarket, bool>? _marketFilter;
        private readonly Func<KalshiUniverseData, bool>? _universeDataFilter;
        private CascadeKalshiDataProvider? _dataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="KalshiUniverse"/> class
        /// </summary>
        public KalshiUniverse(
            UniverseSettings universeSettings,
            TimeSpan refreshInterval,
            string[]? seriesFilter = null,
            string[]? categoryFilter = null,
            Func<KalshiMarket, bool>? marketFilter = null,
            Func<KalshiUniverseData, bool>? universeDataFilter = null)
            : base(
                TimeZones.NewYork,
                CreateDateRule(),
                CreateTimeRule(),
                dt => Enumerable.Empty<Symbol>(),  // Placeholder - we override SelectSymbols
                universeSettings)
        {
            _seriesFilter = seriesFilter;
            _categoryFilter = categoryFilter;
            _marketFilter = marketFilter;
            _universeDataFilter = universeDataFilter;
        }

        private static IDateRule CreateDateRule()
        {
            // Fire every day
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
            // Fire at midnight New York time
            return new FuncTimeRule("Midnight", dates =>
                dates.Select(d => d.ConvertToUtc(TimeZones.NewYork)));
        }

        /// <summary>
        /// Performs universe selection by fetching markets from API
        /// </summary>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return FetchActiveMarkets(utcTime);
        }

        /// <summary>
        /// Parse ISO timestamp string to DateTime
        /// </summary>
        private static DateTime? ParseIsoTime(string? isoTime)
        {
            if (string.IsNullOrEmpty(isoTime))
            {
                return null;
            }

            if (DateTime.TryParse(isoTime, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Check if a market was open (tradeable) at a given time
        /// </summary>
        private static bool IsMarketOpenAt(KalshiMarket market, DateTime localTime)
        {
            var openTime = ParseIsoTime(market.OpenTime);
            var closeTime = ParseIsoTime(market.CloseTime);

            if (openTime == null || closeTime == null)
            {
                return false;
            }

            return openTime.Value <= localTime && closeTime.Value >= localTime;
        }

        private List<Symbol> FetchActiveMarkets(DateTime utcTime)
        {
            // Get or create data provider
            _dataProvider ??= new CascadeKalshiDataProvider();

            var symbols = new List<Symbol>();
            var localTime = utcTime.ConvertFromUtc(TimeZones.NewYork);

            // Determine if we're backtesting (utcTime is in the past)
            var isBacktest = utcTime < DateTime.UtcNow.AddHours(-1);

            try
            {
                List<KalshiMarket> markets;

                if (_seriesFilter != null && _seriesFilter.Length > 0)
                {
                    // Fetch markets for each series
                    markets = new List<KalshiMarket>();
                    foreach (var series in _seriesFilter)
                    {
                        List<KalshiMarket> seriesMarkets;
                        if (isBacktest)
                        {
                            // For backtesting, get markets that were active on this date
                            // Use GetMarketsForDateRange which fetches markets closing on/after this date
                            seriesMarkets = _dataProvider.GetMarketsForDateRange(
                                localTime.Date,
                                localTime.Date.AddDays(1),
                                seriesTicker: series);

                            // Filter to markets that were actually open on this date
                            seriesMarkets = seriesMarkets.Where(m => IsMarketOpenAt(m, localTime)).ToList();
                        }
                        else
                        {
                            seriesMarkets = _dataProvider.GetAvailableMarkets(status: "open", seriesTicker: series);
                        }
                        markets.AddRange(seriesMarkets);
                    }
                }
                else
                {
                    if (isBacktest)
                    {
                        // For backtesting without series filter
                        markets = _dataProvider.GetMarketsForDateRange(
                            localTime.Date,
                            localTime.Date.AddDays(1));

                        markets = markets.Where(m => IsMarketOpenAt(m, localTime)).ToList();
                    }
                    else
                    {
                        // Fetch all open markets for live trading
                        markets = _dataProvider.GetAvailableMarkets(status: "open");
                    }
                }

                // Apply category filter if specified
                if (_categoryFilter != null && _categoryFilter.Length > 0)
                {
                    markets = markets.Where(m =>
                        _categoryFilter.Any(c => string.Equals(m.Category, c, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply custom market filter if specified
                if (_marketFilter != null)
                {
                    markets = markets.Where(_marketFilter).ToList();
                }

                // Apply universe data filter if specified
                if (_universeDataFilter != null)
                {
                    markets = markets.Where(m =>
                    {
                        var universeData = KalshiUniverseData.FromMarket(m, localTime);
                        return _universeDataFilter(universeData);
                    }).ToList();
                }

                // Convert to symbols and register close times/results
                foreach (var market in markets)
                {
                    var symbol = _dataProvider.CreateSymbol(market.Ticker);
                    symbols.Add(symbol);

                    // Register close time in the settlement registry
                    var closeTime = ParseIsoTime(market.CloseTime);
                    if (closeTime.HasValue)
                    {
                        PredictionMarketSettlementRegistry.SetDelistingDate(symbol, closeTime.Value);
                    }

                    // Register settlement result if market is settled
                    if (!string.IsNullOrEmpty(market.Result))
                    {
                        var result = market.Result.ToLowerInvariant() switch
                        {
                            "yes" => PredictionMarketSettlementResult.Yes,
                            "no" => PredictionMarketSettlementResult.No,
                            _ => PredictionMarketSettlementResult.Pending
                        };
                        PredictionMarketSettlementRegistry.SetResult(symbol, result);
                    }
                }

                Log.Trace($"KalshiUniverse: Selected {symbols.Count} markets (backtest={isBacktest}, date={localTime:yyyy-MM-dd})");
            }
            catch (Exception ex)
            {
                Log.Error($"KalshiUniverse: Error fetching markets: {ex.Message}");
            }

            return symbols;
        }
    }
}
