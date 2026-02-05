/*
 * Cascade Labs - ThetaData Coarse Universe Generator
 * Generates LEAN-compatible coarse universe CSV files from ThetaData API
 */

using System.Collections.Concurrent;
using System.Globalization;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Rest;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.SubscriptionPlans;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Generates LEAN-compatible coarse universe files from ThetaData API data
    /// Output format: equity/usa/fundamental/coarse/{date}.csv
    /// </summary>
    public class ThetaDataCoarseUniverseGenerator : IDisposable
    {
        private readonly CascadeThetaDataRestClient _restClient;
        private readonly IFactorFileProvider _factorFileProvider;
        private readonly string _outputDirectory;
        private readonly int _maxConcurrentTickers;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThetaDataCoarseUniverseGenerator"/> class
        /// </summary>
        /// <param name="factorFileProvider">Factor file provider for price/split factors</param>
        /// <param name="outputDirectory">Directory to write coarse universe files</param>
        public ThetaDataCoarseUniverseGenerator(IFactorFileProvider factorFileProvider, string outputDirectory)
        {
            _factorFileProvider = factorFileProvider;
            _outputDirectory = outputDirectory;
            _maxConcurrentTickers = Config.GetInt("thetadata-coarse-max-concurrent", 10);

            var subscriptionPlan = GetSubscriptionPlan();
            _restClient = new CascadeThetaDataRestClient(subscriptionPlan.RateGate!);

            Log.Trace($"ThetaDataCoarseUniverseGenerator: Initialized with output directory: {outputDirectory}");
        }

        /// <summary>
        /// Initializes a new instance with an existing REST client
        /// </summary>
        /// <param name="restClient">The REST client to use</param>
        /// <param name="factorFileProvider">Factor file provider for price/split factors</param>
        /// <param name="outputDirectory">Directory to write coarse universe files</param>
        public ThetaDataCoarseUniverseGenerator(
            CascadeThetaDataRestClient restClient,
            IFactorFileProvider factorFileProvider,
            string outputDirectory)
        {
            _restClient = restClient;
            _factorFileProvider = factorFileProvider;
            _outputDirectory = outputDirectory;
            _maxConcurrentTickers = Config.GetInt("thetadata-coarse-max-concurrent", 10);

            Log.Trace($"ThetaDataCoarseUniverseGenerator: Initialized with provided REST client");
        }

        /// <summary>
        /// Generates coarse universe files for the specified date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        public void Generate(DateTime startDate, DateTime endDate)
        {
            Log.Trace($"ThetaDataCoarseUniverseGenerator: Starting generation for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            // Ensure output directory exists
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            // Fetch all stock tickers
            var tickers = _restClient.GetStockRoots().ToList();
            Log.Trace($"ThetaDataCoarseUniverseGenerator: Found {tickers.Count} stock tickers");

            // Process each date
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                GenerateForDate(date, tickers);
            }

            Log.Trace("ThetaDataCoarseUniverseGenerator: Generation complete");
        }

        /// <summary>
        /// Generates a coarse universe file for a single date
        /// </summary>
        /// <param name="date">The date to generate for</param>
        /// <param name="tickers">List of tickers to process</param>
        private void GenerateForDate(DateTime date, List<string> tickers)
        {
            Log.Debug($"ThetaDataCoarseUniverseGenerator: Processing {date:yyyy-MM-dd}");

            var coarseRows = new ConcurrentBag<string>();
            var processedCount = 0;
            var errorCount = 0;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxConcurrentTickers
            };

            Parallel.ForEach(tickers, parallelOptions, ticker =>
            {
                try
                {
                    var row = ProcessTicker(ticker, date);
                    if (row != null)
                    {
                        coarseRows.Add(row);
                    }

                    var count = Interlocked.Increment(ref processedCount);
                    if (count % 500 == 0)
                    {
                        Log.Debug($"ThetaDataCoarseUniverseGenerator: Processed {count}/{tickers.Count} tickers for {date:yyyy-MM-dd}");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref errorCount);
                    Log.Debug($"ThetaDataCoarseUniverseGenerator: Error processing {ticker}: {ex.Message}");
                }
            });

            if (coarseRows.Count > 0)
            {
                WriteCoarseFile(date, coarseRows.OrderBy(r => r).ToList());
                Log.Trace($"ThetaDataCoarseUniverseGenerator: Wrote {coarseRows.Count} entries for {date:yyyy-MM-dd} ({errorCount} errors)");
            }
            else
            {
                Log.Debug($"ThetaDataCoarseUniverseGenerator: No data for {date:yyyy-MM-dd}");
            }
        }

        /// <summary>
        /// Processes a single ticker for a date and returns the coarse row if data exists
        /// </summary>
        private string? ProcessTicker(string ticker, DateTime date)
        {
            // Fetch EOD data for the ticker on this date
            var eodData = _restClient.GetEndOfDayData(ticker, date, date).FirstOrDefault();

            // Skip if no data or invalid
            if (eodData.Close <= 0 || eodData.Volume <= 0)
            {
                return null;
            }

            // Generate SecurityIdentifier for this ticker
            // Use the date as the first date since we don't have map file data
            var sid = SecurityIdentifier.GenerateEquity(date, ticker, Market.USA);
            var symbol = new Symbol(sid, ticker);

            // Get price and split factors from factor file provider
            var priceFactor = 1m;
            var splitFactor = 1m;

            try
            {
                var factorFile = _factorFileProvider.Get(symbol);
                if (factorFile is CorporateFactorProvider corporateFactorProvider)
                {
                    var factors = corporateFactorProvider.GetScalingFactors(date);
                    priceFactor = factors?.PriceFactor.Normalize() ?? 1m;
                    splitFactor = factors?.SplitFactor.Normalize() ?? 1m;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"ThetaDataCoarseUniverseGenerator: Could not get factors for {ticker}: {ex.Message}");
            }

            // Calculate dollar volume
            var dollarVolume = Math.Truncate((double)(eodData.Close * eodData.Volume));

            // ThetaData doesn't provide fundamental data
            var hasFundamentalData = false;

            // Format: sid,symbol,close,volume,dollar_volume,has_fundamental_data,price_factor,split_factor
            return string.Join(",",
                sid.ToString(),
                ticker,
                eodData.Close.Normalize().ToString(CultureInfo.InvariantCulture),
                decimal.ToInt64(eodData.Volume).ToString(CultureInfo.InvariantCulture),
                dollarVolume.ToString(CultureInfo.InvariantCulture),
                hasFundamentalData.ToString(),
                priceFactor.ToString(CultureInfo.InvariantCulture),
                splitFactor.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes the coarse universe file for a date
        /// </summary>
        private void WriteCoarseFile(DateTime date, List<string> rows)
        {
            var filename = $"{date.ToString(DateFormat.EightCharacter, CultureInfo.InvariantCulture)}.csv";
            var filePath = Path.Combine(_outputDirectory, filename);

            File.WriteAllLines(filePath, rows);
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _restClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
