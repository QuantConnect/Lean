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

using QuantConnect.Logging;
using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages.Samco;
using QuantConnect.Data;
using QuantConnect.Securities;

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
                Log.Error("SamcoDataDownloader ERROR: '--tickers=', --securityType, '--market' or '--resolution=' parameter is missing");
                Log.Error("--tickers=eg JSWSTEEL,TCS,INFY");
                Log.Error("--market=MCX/NSE/NFO/CDS/BSE");
                Log.Error("--security-type=Equity/Future/Option");
                Log.Error("--resolution=Minute/Hour/Daily/Tick");
                Environment.Exit(1);
            }
            try
            {
                var _samcoAPI = new SamcoBrokerageAPI();
                _samcoAPI.Authorize(Config.Get("samco.client-id"), Config.Get("samco.client-password"), Config.Get("samco.year-of-birth"));

                var castResolution = (Resolution)Enum.Parse(typeof(Resolution), resolution);
                var castSecurityType = (SecurityType)Enum.Parse(typeof(SecurityType), securityType);

                if (castSecurityType == SecurityType.Forex || castSecurityType == SecurityType.Cfd || castSecurityType == SecurityType.Crypto || castSecurityType == SecurityType.Base)
                {
                    throw new ArgumentException("Invalid security type: " + castSecurityType);
                }

                if (startDate >= endDate)
                {
                    throw new ArgumentException("Invalid date range specified");
                }

                // Load settings from config.json and create downloader
                var dataDirectory = Globals.DataFolder;
                var marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
                var symbolMapper = new SamcoSymbolMapper();

                foreach (var pair in tickers)
                {
                    // Download data
                    var pairObject = Symbol.Create(pair, castSecurityType, market);

                    var exchangeTimeZone = marketHoursDatabase.GetExchangeHours(market, pairObject, castSecurityType).TimeZone;
                    var exchange = symbolMapper.GetDefaultExchange(pairObject);

                    var start = startDate.ConvertTo(DateTimeZone.Utc, exchangeTimeZone);
                    var end = endDate.ConvertTo(DateTimeZone.Utc, exchangeTimeZone);

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
                            history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, exchange, start, end).ToList();
                            timeSpan = Time.OneMinute;
                            break;

                        case Resolution.Hour:
                            history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, exchange, start, end).ToList();
                            timeSpan = Time.OneHour;
                            break;

                        case Resolution.Daily:
                            history = _samcoAPI.GetIntradayCandles(pairObject.ID.Symbol, exchange, start, end).ToList();
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
