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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Brokerages.Oanda;
using Environment = QuantConnect.Brokerages.Oanda.Environment;

namespace QuantConnect.ToolBox.OandaDownloader
{
    /// <summary>
    /// Oanda Data Downloader class
    /// </summary>
    public class OandaDataDownloader : IDataDownloader
    {
        private readonly OandaBrokerage _brokerage;
        private readonly OandaSymbolMapper _symbolMapper = new OandaSymbolMapper();

        /// <summary>
        /// Initializes a new instance of the <see cref="OandaDataDownloader"/> class
        /// </summary>
        public OandaDataDownloader(string accessToken, string accountId)
        {
            // Set Oanda account credentials
            _brokerage = new OandaBrokerage(null,
                null,
                Environment.Practice,
                accessToken,
                accountId);
        }

        /// <summary>
        /// Checks if downloader can get the data for the Lean symbol
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>Returns true if the symbol is available</returns>
        public bool HasSymbol(string symbol)
        {
            return _symbolMapper.IsKnownLeanSymbol(Symbol.Create(symbol, GetSecurityType(symbol), Market.Oanda));
        }

        /// <summary>
        /// Gets the security type for the specified Lean symbol
        /// </summary>
        /// <param name="symbol">The Lean symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetSecurityType(string symbol)
        {
            return _symbolMapper.GetLeanSecurityType(symbol);
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
            if (!_symbolMapper.IsKnownLeanSymbol(symbol))
                throw new ArgumentException("Invalid symbol requested: " + symbol.Value);

            if (resolution == Resolution.Tick)
                throw new NotSupportedException("Resolution not available: " + resolution);

            if (symbol.ID.SecurityType != SecurityType.Forex && symbol.ID.SecurityType != SecurityType.Cfd)
                throw new NotSupportedException("SecurityType not available: " + symbol.ID.SecurityType);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal than the start date.");

            var barsTotalInPeriod = new List<QuoteBar>();
            var barsToSave = new List<QuoteBar>();

            // set the starting date/time
            var date = startUtc;
            var startDateTime = date;

            // loop until last date
            while (startDateTime <= endUtc.AddDays(1))
            {
                // request blocks of 5-second bars with a starting date/time
                var bars = _brokerage.DownloadQuoteBars(symbol, startDateTime, endUtc.AddDays(1), Resolution.Second, DateTimeZone.Utc).ToList();
                if (bars.Count == 0)
                    break;

                var groupedBars = GroupBarsByDate(bars);

                if (groupedBars.Count > 1)
                {
                    // we received more than one day, so we save the completed days and continue
                    while (groupedBars.Count > 1)
                    {
                        var currentDate = groupedBars.Keys.First();
                        if (currentDate > endUtc)
                            break;

                        barsToSave.AddRange(groupedBars[currentDate]);

                        barsTotalInPeriod.AddRange(barsToSave);

                        barsToSave.Clear();

                        // remove the completed date
                        groupedBars.Remove(currentDate);
                    }

                    // update the current date
                    date = groupedBars.Keys.First();

                    if (date <= endUtc)
                    {
                        barsToSave.AddRange(groupedBars[date]);
                    }
                }
                else
                {
                    var currentDate = groupedBars.Keys.First();
                    if (currentDate > endUtc)
                        break;

                    // update the current date
                    date = currentDate;

                    barsToSave.AddRange(groupedBars[date]);
                }

                // calculate the next request datetime (next 5-sec bar time)
                startDateTime = bars[bars.Count - 1].Time.AddSeconds(5);
            }

            if (barsToSave.Count > 0)
            {
                barsTotalInPeriod.AddRange(barsToSave);
            }

            switch (resolution)
            {
                case Resolution.Second:
                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    foreach (var bar in AggregateBars(symbol, barsTotalInPeriod, resolution.ToTimeSpan()))
                    {
                        yield return bar;
                    }
                    break;
            }
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

        /// <summary>
        /// Groups a list of bars into a dictionary keyed by date
        /// </summary>
        /// <param name="bars"></param>
        /// <returns></returns>
        private static SortedDictionary<DateTime, List<QuoteBar>> GroupBarsByDate(IEnumerable<QuoteBar> bars)
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
    }
}
