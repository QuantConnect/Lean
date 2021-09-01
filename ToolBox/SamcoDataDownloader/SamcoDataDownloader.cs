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

using QuantConnect.Brokerages.Samco;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Generic;

namespace QuantConnect.ToolBox.SamcoDataDownloader
{
    public class SamcoDataDownloaderProgram
    {
        /// <summary>
        /// Samco Data Downloader Toolbox Project For LEAN Algorithmic Trading Engine. By Balamurali
        /// Pandranki a.k.a @itsbalamurali
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
                _samcoAPI.Authorize(Config.Get("samco-client-id"), Config.Get("samco-client-password"), Config.Get("samco-year-of-birth"));

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
                    var exchange = symbolMapper.GetDefaultExchange(pairObject);
                    var isIndex = pairObject.SecurityType == SecurityType.Index;

                    // Write data
                    var writer = new LeanDataWriter(castResolution, pairObject, dataDirectory);
                    IList<TradeBar> fileEnum = new List<TradeBar>();
                    if (castResolution == Resolution.Tick)
                    {
                        throw new ArgumentException("Samco Doesn't support tick resolution");
                    }
                    var history = _samcoAPI.GetIntradayCandles(pairObject, exchange, startDate, endDate, isIndex: isIndex);

                    foreach (var bar in history)
                    {
                        fileEnum.Add(bar);
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
