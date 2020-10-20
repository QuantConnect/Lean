using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Brokerages.Zerodha;
using QuantConnect.Brokerages.Zerodha.Messages;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.ToolBox.ZerodhaDownloader
{
    public class ZerodhaDataDownloaderProgram
    {
        private static readonly string _apiKey = Config.Get("zerodha-api-key");
        private static readonly string _accessToken = Config.Get("zerodha-access-token");

        /// <summary>
        /// Zerodha Historical Data Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// By @itsbalamurali
        /// </summary>
        public static void ZerodhaDataDownloader(IList<string> tickers, string market, string resolution, string securityType, DateTime startDate, DateTime endDate)
        {
            if (resolution.IsNullOrEmpty() || tickers.IsNullOrEmpty())
            {
                Console.WriteLine("ZerodhaDataDownloader ERROR: '--tickers=', --securityType, '--market' or '--resolution=' parameter is missing");
                Console.WriteLine("--tickers=eg JSWSTEEL,TCS,INFY");
                Console.WriteLine("--market=MCX/NSE/NFO/CDS/BSE");
                Console.WriteLine("--security-type=Equity/Future/Option/Commodity");
                Console.WriteLine("--resolution=Minute/Hour/Daily/Tick");
                Environment.Exit(1);
            }
            try
            {
                var _kite = new Kite(_apiKey,_accessToken);
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
                        var history = new List<Historical>();
                        var timeSpan = new TimeSpan();
                        switch (castResolution)
                        {
                            case Resolution.Tick:
                                throw new ArgumentException("Zerodha Doesn't support tick resolution");

                            case Resolution.Minute:
                                history = _kite.GetHistoricalData(pairObject.ID.Symbol, start, end, "minute").ToList();
                                timeSpan = Time.OneMinute;
                                break;

                            case Resolution.Hour:
                                history = _kite.GetHistoricalData(pairObject.ID.Symbol, start, end, "60minute").ToList();
                                timeSpan = Time.OneHour;
                                break;

                            case Resolution.Daily:
                                history = _kite.GetHistoricalData(pairObject.ID.Symbol, start, end, "day").ToList();
                                timeSpan = Time.OneDay;
                                break;
                        }

                        foreach (var bar in history)
                        {
                            var linedata = new TradeBar(bar.TimeStamp, pairObject, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume, timeSpan);
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
