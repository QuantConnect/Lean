/*
 * Cascade Labs - ThetaData Factor File Provider
 * Fetches corporate actions from ThetaData API and generates LEAN-compatible factor files
 */

using System.Collections.Concurrent;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Factor file provider that fetches corporate actions from ThetaData API
    /// and generates LEAN-compatible factor files on demand
    /// </summary>
    public class ThetaDataFactorFileProvider : IFactorFileProvider
    {
        private IMapFileProvider _mapFileProvider;
        private IDataProvider _dataProvider;
        private CascadeThetaDataRestClient _restClient;
        private LocalDiskFactorFileProvider _localProvider;
        private readonly ConcurrentDictionary<Symbol, object> _generationLocks;
        private readonly object _initLock = new();
        private bool _initialized;

        /// <summary>
        /// Default start date for fetching corporate actions.
        /// Set to 2020 to align with ThetaData's CTA tape coverage (2020-01-01)
        /// and our alternative data which starts in 2022.
        /// </summary>
        private static readonly DateTime DefaultStartDate = new DateTime(2020, 1, 1);

        public ThetaDataFactorFileProvider()
        {
            _generationLocks = new ConcurrentDictionary<Symbol, object>();
        }

        /// <summary>
        /// Initializes the provider with map file provider and data provider
        /// </summary>
        public void Initialize(IMapFileProvider mapFileProvider, IDataProvider dataProvider)
        {
            lock (_initLock)
            {
                if (_initialized) return;

                _mapFileProvider = mapFileProvider;
                _dataProvider = dataProvider;

                // Initialize the local provider for reading existing factor files
                _localProvider = new LocalDiskFactorFileProvider();
                _localProvider.Initialize(mapFileProvider, dataProvider);

                // Initialize REST client
                var subscriptionPlan = GetSubscriptionPlan();
                _restClient = new CascadeThetaDataRestClient(subscriptionPlan.RateGate!);

                _initialized = true;
                Log.Trace("ThetaDataFactorFileProvider: Initialized");
            }
        }

        /// <summary>
        /// Gets a factor file for the specified symbol.
        /// If a factor file exists on disk, uses that. Otherwise fetches from ThetaData API.
        /// </summary>
        public IFactorProvider Get(Symbol symbol)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("ThetaDataFactorFileProvider has not been initialized");
            }

            // Only support equities
            if (symbol.SecurityType != SecurityType.Equity)
            {
                return null;
            }

            var factorSymbol = symbol.GetFactorFileSymbol();
            var factorFilePath = GetFactorFilePath(symbol);

            // Fast path: file already exists on disk
            if (File.Exists(factorFilePath))
            {
                Log.Debug($"ThetaDataFactorFileProvider: Using existing factor file for {symbol.Value}");
                return _localProvider.Get(symbol);
            }

            // Slow path: need to generate - use per-symbol lock to prevent duplicate API calls
            var symbolLock = _generationLocks.GetOrAdd(factorSymbol, _ => new object());
            lock (symbolLock)
            {
                // Double-check: another thread may have generated while we waited
                if (File.Exists(factorFilePath))
                {
                    Log.Debug($"ThetaDataFactorFileProvider: Using existing factor file for {symbol.Value}");
                    return _localProvider.Get(symbol);
                }

                // Generate from ThetaData API
                Log.Trace($"ThetaDataFactorFileProvider: Generating factor file for {symbol.Value} from ThetaData API");
                return GenerateFactorFile(symbol);
            }
        }

        /// <summary>
        /// Generates a factor file by fetching corporate actions and daily data from ThetaData
        /// </summary>
        private CorporateFactorProvider GenerateFactorFile(Symbol symbol)
        {
            var ticker = symbol.Value;
            var startDate = DefaultStartDate;
            var endDate = DateTime.UtcNow.Date;

            try
            {
                // Fetch splits from ThetaData
                var splits = FetchSplits(ticker, startDate, endDate);
                Log.Debug($"ThetaDataFactorFileProvider: Found {splits.Count} splits for {ticker}");

                // Fetch dividends from ThetaData
                var dividends = FetchDividends(ticker, startDate, endDate);
                Log.Debug($"ThetaDataFactorFileProvider: Found {dividends.Count} dividends for {ticker}");

                // If no corporate actions, return a minimal factor file
                if (splits.Count == 0 && dividends.Count == 0)
                {
                    var minimalFactorFile = CreateMinimalFactorFile(symbol);
                    WriteFactorFile(symbol, minimalFactorFile);
                    return minimalFactorFile;
                }

                // Fetch daily price data for reference prices
                var dailyData = FetchDailyData(ticker, startDate, endDate);
                Log.Debug($"ThetaDataFactorFileProvider: Found {dailyData.Count} daily bars for {ticker}");

                if (dailyData.Count == 0)
                {
                    Log.Error($"ThetaDataFactorFileProvider: No daily data available for {ticker}, cannot generate factor file");
                    return CreateMinimalFactorFile(symbol);
                }

                // Convert to LEAN types
                var corporateActions = ConvertToCorporateActions(symbol, splits, dividends, dailyData);

                // Get exchange hours for factor calculation
                var exchangeHours = MarketHoursDatabase.FromDataFolder()
                    .GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

                // Generate factor file using CorporateFactorProvider.Apply
                var factorFile = GenerateFactorFileFromCorporateActions(symbol, corporateActions, dailyData, exchangeHours);

                // Write to disk
                WriteFactorFile(symbol, factorFile);

                return factorFile;
            }
            catch (Exception ex)
            {
                Log.Error($"ThetaDataFactorFileProvider: Error generating factor file for {ticker}: {ex.Message}");
                return CreateMinimalFactorFile(symbol);
            }
        }

        /// <summary>
        /// Fetches and deduplicates splits from ThetaData API
        /// </summary>
        private List<SplitResponse> FetchSplits(string ticker, DateTime startDate, DateTime endDate)
        {
            var allSplits = _restClient.GetSplits(ticker, startDate, endDate).ToList();

            // Filter out splits with invalid dates (API may return '0' for unknown dates)
            // and deduplicate by split_date (API may return same event on multiple query dates)
            var uniqueSplits = allSplits
                .Where(s => s.SplitDate != DateTime.MinValue)
                .GroupBy(s => s.SplitDate)
                .Select(g => g.First())
                .OrderBy(s => s.SplitDate)
                .ToList();

            return uniqueSplits;
        }

        /// <summary>
        /// Fetches and deduplicates dividends from ThetaData API
        /// </summary>
        private List<DividendResponse> FetchDividends(string ticker, DateTime startDate, DateTime endDate)
        {
            var allDividends = _restClient.GetDividends(ticker, startDate, endDate).ToList();

            // Filter out dividends with invalid ex_date (API may return '0' for unknown dates)
            // and deduplicate by ex_date (API may return same event on multiple query dates)
            var uniqueDividends = allDividends
                .Where(d => d.ExDate != DateTime.MinValue)
                .GroupBy(d => d.ExDate)
                .Select(g => g.First())
                .OrderBy(d => d.ExDate)
                .ToList();

            return uniqueDividends;
        }

        /// <summary>
        /// Fetches daily price data from ThetaData API
        /// </summary>
        private List<EndOfDayReportResponse> FetchDailyData(string ticker, DateTime startDate, DateTime endDate)
        {
            var dailyData = _restClient.GetEndOfDayData(ticker, startDate, endDate)
                .Where(e => e.Close > 0) // Filter out zero closes
                .OrderByDescending(e => e.Date)
                .ToList();

            return dailyData;
        }

        /// <summary>
        /// Converts ThetaData responses to LEAN Dividend and Split objects
        /// </summary>
        private List<BaseData> ConvertToCorporateActions(
            Symbol symbol,
            List<SplitResponse> splits,
            List<DividendResponse> dividends,
            List<EndOfDayReportResponse> dailyData)
        {
            var corporateActions = new List<BaseData>();

            // Create a lookup for daily closes by date for reference prices
            var closeLookup = dailyData.ToDictionary(
                d => d.Date.Date,
                d => d.Close);

            // Convert splits
            foreach (var split in splits)
            {
                // Find reference price (previous day's close)
                var referencePrice = FindReferencePrice(split.SplitDate, closeLookup);

                if (referencePrice > 0)
                {
                    corporateActions.Add(new Split(
                        symbol,
                        split.SplitDate,
                        referencePrice,
                        split.SplitFactor,
                        SplitType.SplitOccurred
                    ));
                }
            }

            // Convert dividends
            foreach (var dividend in dividends)
            {
                // Find reference price (previous day's close)
                var referencePrice = FindReferencePrice(dividend.ExDate, closeLookup);

                if (referencePrice > 0 && dividend.DividendAmount > 0)
                {
                    corporateActions.Add(new Dividend(
                        symbol,
                        dividend.ExDate,
                        dividend.DividendAmount,
                        referencePrice
                    ));
                }
            }

            return corporateActions.OrderBy(c => c.Time).ToList();
        }

        /// <summary>
        /// Finds the reference price (close from the trading day before the event)
        /// </summary>
        private decimal FindReferencePrice(DateTime eventDate, Dictionary<DateTime, decimal> closeLookup)
        {
            // Look for close prices in the days before the event
            for (int i = 1; i <= 5; i++)
            {
                var lookupDate = eventDate.AddDays(-i).Date;
                if (closeLookup.TryGetValue(lookupDate, out var close))
                {
                    return close;
                }
            }

            return 0m;
        }

        /// <summary>
        /// Generates factor file from corporate actions using CorporateFactorProvider.Apply
        /// </summary>
        private CorporateFactorProvider GenerateFactorFileFromCorporateActions(
            Symbol symbol,
            List<BaseData> corporateActions,
            List<EndOfDayReportResponse> dailyData,
            SecurityExchangeHours exchangeHours)
        {
            // Create initial factor file with sentinel row
            var initialRows = new List<CorporateFactorRow>
            {
                new CorporateFactorRow(Time.EndOfTime, 1m, 1m, 0m)
            };

            // Get earliest data date for the first factor file row
            var earliestDate = dailyData.Count > 0
                ? dailyData.Min(d => d.Date).Date
                : DefaultStartDate;

            // Add sentinel row for earliest date
            initialRows.Add(new CorporateFactorRow(earliestDate, 1m, 1m, 0m));

            var factorFile = new CorporateFactorProvider(symbol.Value, initialRows);

            // Apply corporate actions if any
            if (corporateActions.Count > 0)
            {
                factorFile = factorFile.Apply(corporateActions, exchangeHours);
            }

            return factorFile;
        }

        /// <summary>
        /// Creates a minimal factor file with no adjustments (all factors = 1)
        /// </summary>
        private CorporateFactorProvider CreateMinimalFactorFile(Symbol symbol)
        {
            var rows = new List<CorporateFactorRow>
            {
                new CorporateFactorRow(Time.EndOfTime, 1m, 1m, 0m),
                new CorporateFactorRow(DefaultStartDate, 1m, 1m, 0m)
            };

            return new CorporateFactorProvider(symbol.Value, rows);
        }

        /// <summary>
        /// Writes the factor file to the standard LEAN data directory
        /// </summary>
        private void WriteFactorFile(Symbol symbol, CorporateFactorProvider factorFile)
        {
            var filePath = GetFactorFilePath(symbol);
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            File.WriteAllLines(filePath, factorFile.GetFileFormat());
            Log.Trace($"ThetaDataFactorFileProvider: Wrote factor file to {filePath}");
        }

        /// <summary>
        /// Gets the path where factor file should be stored
        /// </summary>
        private static string GetFactorFilePath(Symbol symbol)
        {
            return LeanData.GenerateRelativeFactorFilePath(symbol);
        }

        /// <summary>
        /// Gets the subscription plan for the REST client
        /// </summary>
        private static ISubscriptionPlan GetSubscriptionPlan()
        {
            var pricePlan = Config.Get("thetadata-subscription-plan", "Pro");

            return pricePlan.ToLowerInvariant() switch
            {
                "free" => new FreeSubscriptionPlan(),
                "value" => new ValueSubscriptionPlan(),
                "standard" => new StandardSubscriptionPlan(),
                "pro" => new ProSubscriptionPlan(),
                _ => new StandardSubscriptionPlan()
            };
        }
    }
}
