using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Brokerages.Samco;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.ToolBox.SamcoDownloader
{
    public class SamcoDataDownloaderProgram
    {
        /// <summary>
        /// Samco Data Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// By @itsbalamurali
        /// </summary>
        public static void SamcoDataDownloader(IList<string> tickers, string market, string resolution, string securityType, DateTime startDate, DateTime endDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("SamcoDataDownloader ERROR: '--tickers=', --securityType, '--market' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg JSWSTEEL,TCS,INFY");
                Console.WriteLine("--market=MCX/NSE/NFO/CDS/BSE");
                Console.WriteLine("--security-type=Equity/Future/Option/Commodity");
                Console.WriteLine("--resolution=Minute/Hour/Daily/Tick");
                Environment.Exit(1);
            }
            try
            {
                var restClient = new RestClient("https://api.stocknote.com");
                var _samcoAPI = new SamcoBrokerageAPI(restClient);
                //TODO: get this from cli params
                //_samcoAPI.Authorize("dpid", "pass", "dob");

                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var castSecurityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityType);

                // Load settings from config.json and create downloader
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                foreach (var pair in tickers)
                {
                    // Download data
                    var pairObject = Symbol.Create(pair, castSecurityType, market);

                    if (pairObject.ID.SecurityType != SecurityType.Forex || pairObject.ID.SecurityType != SecurityType.Cfd || pairObject.ID.SecurityType != SecurityType.Crypto || pairObject.ID.SecurityType == SecurityType.Base)
                    {

                        if (pairObject.ID.SecurityType == SecurityType.Forex || pairObject.ID.SecurityType == SecurityType.Cfd || pairObject.ID.SecurityType == SecurityType.Crypto || pairObject.ID.SecurityType == SecurityType.Base)
                        {
                            throw new ArgumentException("Invalid security type: " + pairObject.ID.SecurityType);
                        }

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
                                history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, market, start, end).ToList();
                                timeSpan = Time.OneMinute;
                                break;

                            case Resolution.Hour:
                                throw new ArgumentException("Samco Doesn't support second resolution");

                            case Resolution.Daily:
                                throw new ArgumentException("Samco Doesn't support second resolution");
                        }

                        foreach (var bar in history)
                        {
                            var linedata = new TradeBar(bar.Time, pairObject, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, timeSpan);
                            fileEnum.Add(linedata);
                        }

                        writer.Write(fileEnum);
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
