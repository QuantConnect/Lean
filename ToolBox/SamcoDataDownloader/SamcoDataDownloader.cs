using QuantConnect.Logging;
using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages.Samco;

namespace QuantConnect.ToolBox.SamcoDataDownloader
{
    public class SamcoDataDownloaderProgram
    {
        /// <summary>
        /// Samco Data Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// By Balamurali Pandranki a.k.a @itsbalamurali
        /// </summary>
        public static void SamcoDataDownloader(IList<string> tickers, string market, string resolution, string securityType, DateTime startDate, DateTime endDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("SamcoDataDownloader ERROR: '--tickers=', --securityType, '--market' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg JSWSTEEL,TCS,INFY");
                Console.WriteLine("--market=MCX/NSE/NFO/CDS/BSE");
                Console.WriteLine("--security-type=Equity/Future/Option");
                Console.WriteLine("--resolution=Minute/Hour/Daily/Tick");
                Environment.Exit(1);
            }
            try
            {
                var _samcoAPI = new SamcoBrokerageAPI();
                _samcoAPI.Authorize(Config.Get("samco.api-key"), Config.Get("samco.api-secret"), Config.Get("samco.year-of-birth"));

                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var castSecurityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityType);

                if (castSecurityType == SecurityType.Forex || castSecurityType == SecurityType.Cfd || castSecurityType == SecurityType.Crypto || castSecurityType == SecurityType.Base)
                {
                    throw new ArgumentException("Invalid security type: " + castSecurityType);
                }

                // Load settings from config.json and create downloader
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                foreach (var pair in tickers)
                {
                    // Download data
                    var pairObject = Symbol.Create(pair, castSecurityType, market);

                    if (startDate >= endDate)
                    {
                        throw new ArgumentException("Invalid date range specified");
                    }

                    var start = startDate.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);
                    var end = endDate.ConvertTo(DateTimeZone.Utc, TimeZones.Kolkata);

                    // Write data
                    var writer = new LeanDataWriter(castResolution, pairObject, dataDirectory);
                    IList<TradeBar> fileEnum = new List<TradeBar>();
                    var history = new List<TradeBar>();
                    var timeSpan = new TimeSpan();
                    switch (castResolution)
                    {
                        case Resolution.Tick:
                            throw new ArgumentException("Samco Doesn't support tick resolution");

                        case Resolution.Minute:
                            history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, market, start, end,"1").ToList();
                            timeSpan = Time.OneMinute;
                            break;

                        case Resolution.Hour:
                            history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, market, start, end,"60").ToList();
                            timeSpan = Time.OneHour;
                            break;

                        case Resolution.Daily:
                            history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, market, start, end,"3600").ToList();
                            timeSpan = Time.OneDay;
                            break;
                    }

                    foreach (var bar in history)
                    {
                        var linedata = new TradeBar(bar.Time, pairObject, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, timeSpan);
                        fileEnum.Add(linedata);
                    }
                    writer.Write(fileEnum);
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}
