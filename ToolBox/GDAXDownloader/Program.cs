using System;
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    class Program
    {
        /// <summary>
        /// GDAX Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                args = new [] { args[0], DateTime.UtcNow.ToString("yyyyMMdd"), args[1] };
            }
            else if (args.Length < 3)
            {
                Console.WriteLine("Usage: GDAX Downloader SYMBOL FROMDATE TODATE");
                Console.WriteLine("FROMDATE = yyyymmdd");
                Console.WriteLine("TODATE = yyyymmdd");
                Environment.Exit(1);
            }

            try
            {
                // Load settings from command line
                var startDate = DateTime.ParseExact(args[1], "yyyyMMdd", CultureInfo.InvariantCulture);
                var endDate = DateTime.ParseExact(args[2], "yyyyMMdd", CultureInfo.InvariantCulture);

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as gdax
                // Create an instance of the downloader
                const string market = Market.GDAX;
                var downloader = new GDAXDownloader();

                // Download the data
                var symbolObject = Symbol.Create(args[0], SecurityType.Crypto, market);
                var data = downloader.Get(symbolObject, Resolution.Hour, startDate, endDate);

                // Save the data
                
                var writer = new LeanDataWriter(Resolution.Hour, symbolObject, dataDirectory, TickType.Quote);
                writer.Write(data);
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Finish data download");
                
            }
            catch (Exception err)
            {
                Log.Error(err);
                Console.WriteLine(err.Message);
                Console.WriteLine(err.InnerException);
                Console.WriteLine(err.StackTrace);
            }
            Console.ReadLine();
        }
    }
}