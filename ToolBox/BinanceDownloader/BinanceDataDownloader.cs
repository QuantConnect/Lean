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

using NodaTime;
using QuantConnect.Brokerages.Binance;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.BinanceDownloader
{
    /// <summary>
    /// Binance Downloader class
    /// </summary>
    public class BinanceDataDownloader : IDataDownloader, IDisposable
    {
        private readonly BinanceBrokerage _brokerage;
        private readonly SymbolPropertiesDatabaseSymbolMapper _symbolMapper = new SymbolPropertiesDatabaseSymbolMapper(Market.Binance);

        /// <summary>
        /// Initializes a new instance of the <see cref="BinanceDataDownloader"/> class
        /// </summary>
        public BinanceDataDownloader()
        {
            _brokerage = new BinanceBrokerage(null, null,null, null);
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (resolution == Resolution.Tick || resolution == Resolution.Second)
            {
                throw new ArgumentException($"Resolution not available: {resolution}");
            }

            if (!_symbolMapper.IsKnownLeanSymbol(symbol))
            {
                throw new ArgumentException($"The ticker {symbol.Value} is not available.");
            }

            if (endUtc < startUtc)
            {
                throw new ArgumentException("The end date must be greater or equal than the start date.");
            }

            var historyRequest = new HistoryRequest(
                startUtc,
                endUtc,
                typeof(TradeBar),
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                DateTimeZone.Utc,
                resolution,
                false,
                false,
                DataNormalizationMode.Adjusted,
                TickType.Trade);

            return _brokerage.GetHistory(historyRequest);
        }

        /// <summary>
        /// Creates Lean Symbol
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns></returns>
        internal Symbol GetSymbol(string ticker)
        {
            return _symbolMapper.GetLeanSymbol(ticker, SecurityType.Crypto, Market.Binance);
        }

        /// <summary>
        /// Aggregates a list of minute bars at the requested resolution
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="bars"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        internal IEnumerable<TradeBar> AggregateBars(Symbol symbol, IEnumerable<TradeBar> bars, TimeSpan resolution)
        {
            return
                (from b in bars
                 group b by b.Time.RoundDown(resolution)
                     into g
                 select new TradeBar
                 {
                     Symbol = symbol,
                     Time = g.Key,
                     Open = g.First().Open,
                     High = g.Max(b => b.High),
                     Low = g.Min(b => b.Low),
                     Close = g.Last().Close,
                     Volume = g.Sum(b => b.Volume),
                     Value = g.Last().Close,
                     DataType = MarketDataType.TradeBar,
                     Period = resolution,
                     EndTime = g.Key.AddMilliseconds(resolution.TotalMilliseconds)
                 });
        }

        public void Dispose()
        {
            _brokerage.Disconnect();
            _brokerage.DisposeSafely();
        }
    }
}
