using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.PolygonDownloader
{
    public static class PolygonDownloaderProgram
    {
        // downloader and timer
        private static PolygonDataDownloader _dataDownloader;
        private static Stopwatch _timer = new Stopwatch();

        // private strings
        private static string _apiKey;
        private static string _exchange;
        private static string _dataDirectory;

        /// <summary>
        /// Initialize the variables
        /// </summary>
        /// <param name="apiKey">Polygon API proprietary key</param>
        /// <param name="exchange">The market/exchange requested data belongs to</param>
        /// <param name="dataDirectory"></param>
        public static void Initialize(string apiKey, string exchange, string dataDirectory = null)
        {
            _apiKey = apiKey;
            _exchange = exchange;
            _dataDirectory = dataDirectory ?? Globals.DataFolder;

            // create polygon downloader
            _dataDownloader = new PolygonDataDownloader(_apiKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tickers"></param>
        /// <param name="resolution"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        public static void PolygonDownloader(
            IList<string> tickers, string resolution, DateTime fromDate, DateTime toDate)
        {
            // Check if all input variables are in place
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("PolygonApiDownloader ERROR: 'tickers' or 'resolution' parameter is missing");
                Environment.Exit(1);
            }
            if (_apiKey.IsNullOrEmpty())
            {
                Console.WriteLine("PolygonApiDownloader ERROR: 'api-key' parameter is missing");
                Environment.Exit(1);
            }

            // Start the timer
            Console.WriteLine("PolygonDownloader Start: {0:u}", DateTime.Now);
            Console.WriteLine();
            _timer = Stopwatch.StartNew();

            try
            {
                var downloadAllResolutions = resolution.ToLower(CultureInfo.InvariantCulture) == "all";
                var dataResolutions = downloadAllResolutions
                    ? new[]
                    {
                        Resolution.Tick, Resolution.Second, Resolution.Minute, Resolution.Hour,
                        Resolution.Daily
                    }
                    : new[]
                    {
                        (Resolution) Enum.Parse(typeof(Resolution), resolution)
                    };

                // obtain and save the data for every requested ticker & resolution
                foreach (var ticker in tickers)
                {
                    var symbolObject = Symbol.Create(ticker, SecurityType.Equity, _exchange);

                    foreach (var rs in dataResolutions)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Downloading [{0}] data for symbol [{1}]...", rs.ToString(), ticker);

                        // data handling is a resolution specific
                        switch (rs)
                        {
                            case Resolution.Tick:
                                // roundnig datetime to a date component (!)
                                DownloadAndSaveTicks(symbolObject, fromDate.Date, toDate.Date);
                                break;

                            case Resolution.Second:
                            case Resolution.Minute:
                            case Resolution.Hour:
                            case Resolution.Daily:
                                // TO- DO

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                // TO- DO
                // --- >> DIVIDENDS AND SPLITS ?? 

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                // Print out timing info
                _timer.Stop();
                Console.WriteLine();
                Console.WriteLine("PolygonDownloader End: {0:u}", DateTime.Now);
                Console.WriteLine("Elapsed time: {0}", _timer.Elapsed);
                Console.WriteLine();

                // Exit message
                Console.WriteLine("Press any key to exit ..");
                Console.ReadLine();
            }
        }

        // There can be a lot of ticks, more than 50 k per day
        // storing all ticks for a long period in one collection takes up a memory
        // so we will download per a single day and immediately save to a file and repeat so iteratively 
        // Apparent benifit of this approach is that processing could be done in parallel in a future.
        private static void DownloadAndSaveTicks(Symbol symbol, DateTime fromDate, DateTime toDate)
        {
            var writer = new LeanDataWriter(Resolution.Tick, symbol, _dataDirectory);
            var currentDate = fromDate;

            while (currentDate <= toDate)
            {
                IEnumerable<BaseData> ticks;
                switch (symbol.SecurityType)
                {
                    case SecurityType.Equity:

                        // if exchange was open on that day
                        if (HelperPolygon.ExchangeHoursEquity.IsDateOpen(currentDate))
                        {
                            Console.WriteLine();
                            Console.WriteLine("Downloading ticks for the day: {0:d}", currentDate);

                            // download
                            ticks = _dataDownloader.DownloadHistoricEquityTrades(symbol, currentDate);
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Weekend/ Holiday: {0:d}", currentDate);

                            // otherwise do nothing - step to the next iteration
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }

                        break;
                        
                    case SecurityType.Forex:
                    case SecurityType.Crypto:
                    // TO- DO

                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                // save
                Console.WriteLine("Saving ticks to data folder <->");
                writer.Write(ticks);

                // and step to next iteration
                currentDate = currentDate.AddDays(1);
            }
        }

    }
}
