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
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.ToolBox.IBDownloader
{
    /// <summary>
    /// IB Downloader class
    /// </summary>
    public class IBDataDownloader : IDataDownloader, IDisposable
    {
        private readonly InteractiveBrokersBrokerage _brokerage;

        /// <summary>
        /// Initializes a new instance of the <see cref="IBDataDownloader"/> class
        /// </summary>
        public IBDataDownloader()
        {
            _brokerage = new InteractiveBrokersBrokerage(null, null, null, null, null);
            _brokerage.Connect();
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
            if (resolution == Resolution.Tick)
                throw new NotSupportedException("Resolution not available: " + resolution);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal than the start date.");

            var historyRequest = new HistoryRequest(
                startUtc,
                endUtc,
                typeof(QuoteBar),
                symbol,
                resolution,
                SecurityExchangeHours.AlwaysOpen(TimeZones.EasternStandard),
                DateTimeZone.Utc,
                resolution,
                false,
                false,
                DataNormalizationMode.Adjusted,
                TickType.Quote);

            var data = _brokerage.GetHistory(historyRequest);

            return data;
        }

        /// <summary>
        /// Returns an IEnumerable of Future/Option contract symbols for the given root ticker
        /// </summary>
        /// <param name="symbol">The Symbol to get futures/options chain for</param>
        /// <param name="includeExpired">Include expired contracts</param>
        public IEnumerable<Symbol> GetChainSymbols(Symbol symbol, bool includeExpired)
        {
            return _brokerage.LookupSymbols(symbol, includeExpired);
        }

        /// <summary>
        /// Downloads historical data from the brokerage and saves it in LEAN format.
        /// </summary>
        /// <param name="symbols">The list of symbols</param>
        /// <param name="tickType">The tick type</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="securityType">The security type</param>
        /// <param name="startTimeUtc">The starting date/time (UTC)</param>
        /// <param name="endTimeUtc">The ending date/time (UTC)</param>
        public void DownloadAndSave(List<Symbol> symbols, Resolution resolution, SecurityType securityType, TickType tickType, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var writer = new LeanDataWriter(Globals.DataFolder, resolution, securityType, tickType);
            writer.DownloadAndSave(_brokerage, symbols, startTimeUtc, endTimeUtc);
        }

        /// <summary>
        /// Groups a list of bars into a dictionary keyed by date
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        private static SortedDictionary<DateTime, List<QuoteBar>> GroupBarsByDate(IList<QuoteBar> bars)
        {
            var groupedBars = new SortedDictionary<DateTime, List<QuoteBar>>();

            foreach (var bar in bars)
            {
                var date = bar.Time.Date;

                if (!groupedBars.ContainsKey(date))
                    groupedBars[date] = new List<QuoteBar>();

                groupedBars[date].Add(bar);
            }

            return groupedBars;
        }

        /// <summary>
        /// Aggregates a list of 5-second bars at the requested resolution
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="bars"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        internal IEnumerable<QuoteBar> AggregateBars(Symbol symbol, IEnumerable<QuoteBar> bars, TimeSpan resolution)
        {
            return
                (from b in bars
                 group b by b.Time.RoundDown(resolution)
                     into g
                 select new QuoteBar
                 {
                     Symbol = symbol,
                     Time = g.Key,
                     Bid = new Bar
                     {
                         Open = g.First().Bid.Open,
                         High = g.Max(b => b.Bid.High),
                         Low = g.Min(b => b.Bid.Low),
                         Close = g.Last().Bid.Close
                     },
                     Ask = new Bar
                     {
                         Open = g.First().Ask.Open,
                         High = g.Max(b => b.Ask.High),
                         Low = g.Min(b => b.Ask.Low),
                         Close = g.Last().Ask.Close
                     }
                 });
        }

        #region Console Helper

        /// <summary>
        /// Draw a progress bar
        /// </summary>
        /// <param name="complete"></param>
        /// <param name="maxVal"></param>
        /// <param name="barSize"></param>
        /// <param name="progressCharacter"></param>
        private static void ProgressBar(long complete, long maxVal, long barSize, char progressCharacter)
        {

            decimal p = (decimal)complete / (decimal)maxVal;
            int chars = (int)Math.Floor(p / ((decimal)1 / (decimal)barSize));
            string bar = string.Empty;
            bar = bar.PadLeft(chars, progressCharacter);
            bar = bar.PadRight(Convert.ToInt32(barSize) - 1);

            Console.Write($"\r[{bar}] {(p * 100).ToStringInvariant("N2")}%");
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_brokerage != null)
            {
                _brokerage.Disconnect();
                _brokerage.Dispose();
            }
        }
    }
}
