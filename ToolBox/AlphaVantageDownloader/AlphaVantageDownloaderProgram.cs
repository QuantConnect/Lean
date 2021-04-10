using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    public static class AlphaVantageDownloaderProgram
    {
        /// <summary>
        /// Primary entry point to the program.
        /// </summary>
        public static void AlphaVantageDownloader(List<string> tickers, string resolution, DateTime fromDate, DateTime toDate, string apiKey)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("AlphaVantageDownloader ERROR: '--tickers=' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg SPY,AAPL");
                Console.WriteLine("--resolution=Minute/Hour/Daily");
                Environment.Exit(1);
            }
            try
            {
                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var startDate = fromDate.ConvertToUtc(TimeZones.NewYork);
                var endDate = toDate.ConvertToUtc(TimeZones.NewYork);

                // fix end date
                endDate = new DateTime(Math.Min(endDate.Ticks, DateTime.Now.AddDays(-1).Ticks));

                // Load settings from config.json
                var dataDirectory = Config.Get("data-folder", "../../../Data");

                // Only Equity for now
                SecurityType securityType = SecurityType.Equity;
                string market = Market.USA;

                var downloader = new AlphaVantageDataDownloader(apiKey);
                foreach (var ticker in tickers)
                {
                    // Download the data
                    var symbol = Symbol.Create(ticker, securityType, market);
                    var data = downloader.Get(symbol, castResolution, startDate, endDate);
                    var bars = data.Cast<TradeBar>().ToList();

                    // Save the data
                    var writer = new LeanDataWriter(castResolution, symbol, dataDirectory);
                    writer.Write(data);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
