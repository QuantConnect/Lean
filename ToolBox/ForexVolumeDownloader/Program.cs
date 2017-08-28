using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Globalization;

namespace QuantConnect.ToolBox.FxcmVolumeDownload
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: FxcmVolumeDownloader SYMBOLS RESOLUTION FROMDATE TODATE");
                Console.WriteLine("SYMBOLS = eg EURUSD,USDJPY\n" +
                                  "\tAvailable pairs:\n" +
                                  "\tEURUSD, USDJPY, GBPUSD, USDCHF, EURCHF, AUDUSD, USDCAD,\n" +
                                  "\tNZDUSD, EURGBP, EURJPY, GBPJPY, EURAUD, EURCAD, AUDJPY");
                Console.WriteLine("RESOLUTION = Minute/Hour/Daily/All");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(exitCode: 1);
            }

            try
            {
                Log.DebuggingEnabled = true;
                Log.LogHandler = new CompositeLogHandler(new ConsoleLogHandler(), new FileLogHandler("FxcmFxVolumeDownloader.log", useTimestampPrefix: false));

                // Load settings from command line
                var tickers = args[0].Split(',');
                var resolutions = new[] { Resolution.Daily };

                if (args[1].ToLower() == "all")
                {
                    resolutions = new[] { Resolution.Daily, Resolution.Hour, Resolution.Minute };
                }
                else
                {
                    resolutions[0] = (Resolution)Enum.Parse(typeof(Resolution), args[1]);
                }

                var startDate = DateTime.ParseExact(args[2], "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(args[3], "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json

                var dataDirectory = Config.Get("data-directory", "../../../Data");
                var downloader = new FxcmVolumeDownloader(dataDirectory);
                foreach (var ticker in tickers)
                {
                    var symbol = Symbol.Create(ticker, SecurityType.Base, Market.FXCM);
                    foreach (var resolution in resolutions)
                    {
                        downloader.Run(symbol, resolution, startDate, endDate);
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
