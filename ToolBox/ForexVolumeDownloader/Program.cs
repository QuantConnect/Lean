using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Globalization;

namespace QuantConnect.ToolBox.FxVolumeDownloader
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: ForexVolumeDownloader SYMBOLS RESOLUTION FROMDATE TODATE");
                Console.WriteLine("SYMBOLS = eg EURUSD,USDJPY\n" +
                                  "\tAvailable pairs:\n" +
                                  "\tEURUSD, USDJPY, GBPUSD, USDCHF, EURCHF, AUDUSD, USDCAD,\n" +
                                  "\tNZDUSD, EURGBP, EURJPY, GBPJPY, EURAUD, EURCAD, AUDJPY");
                Console.WriteLine("RESOLUTION = Minute/Hour/Daily/All");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");

#if DEBUG
                args = new string[] { "EURJPY,GBPJPY,EURAUD,AUDJPY", "Hour", "20140101", "20170101" };
#endif

                Environment.Exit(exitCode: 1);
            }

            try
            {
                var timer = DateTime.Now;
                Log.DebuggingEnabled = true;
                var logHandlers = new ILogHandler[]
                {
                    new ConsoleLogHandler(), new FileLogHandler("FxcmFxVolumeDownloader.log", useTimestampPrefix: false)
                };

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
                //var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "TestingFXVolumeData");

                Market.Add("FXCMForexVolume", identifier: 20);

                var downloader = new ForexVolumeDownloader(dataDirectory);
                foreach (var ticker in tickers)
                {
                    var symbol = Symbol.Create(ticker, SecurityType.Base, Market.Decode(code: 20));
                    foreach (var resolution in resolutions)
                    {
                        downloader.Run(symbol, resolution, startDate, endDate);
                    }
                }
                Console.WriteLine("\n => Timer: {0} milliseconds.", (DateTime.Now - timer).TotalMilliseconds);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}