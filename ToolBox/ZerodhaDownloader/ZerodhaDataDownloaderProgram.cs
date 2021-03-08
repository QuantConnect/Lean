/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

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
                var _kite = new Kite(_apiKey, _accessToken);
                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var castSecurityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityType);

                // Load settings from config.json and create downloader
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                foreach (var pair in tickers)
                {
                    var quoteTicker = market + ":" + pair;
                    var instrumentQuotes = _kite.GetQuote(new string[] { quoteTicker });
                    var quote = instrumentQuotes[quoteTicker];

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

                                if ((end - start).Days > 60)
                                    throw new ArgumentOutOfRangeException("For minutes data Zerodha support 60 days data download");
                                history = _kite.GetHistoricalData(quote.InstrumentToken.ToStringInvariant(), start, end, "minute").ToList();
                                timeSpan = Time.OneMinute;
                                break;

                            case Resolution.Hour:
                                if ((end - start).Days > 400)
                                    throw new ArgumentOutOfRangeException("For daily data Zerodha support 400 days data download");
                                history = _kite.GetHistoricalData(quote.InstrumentToken.ToStringInvariant(), start, end, "60minute").ToList();
                                timeSpan = Time.OneHour;
                                break;

                            case Resolution.Daily:
                                if ((end - start).Days > 400)
                                    throw new ArgumentOutOfRangeException("For daily data Zerodha support 400 days data download");
                                history = _kite.GetHistoricalData(quote.InstrumentToken.ToStringInvariant(), start, end, "day").ToList();
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
                Log.Error($"ZerodhaDataDownloadManager.OnError(): Message: {err.Message} Exception: {err.InnerException}");
            }

        }
    }
}
