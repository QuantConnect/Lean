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
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.IQFeed
{
    public class IQFeedDataDownloader : IDataDownloader
    {
        private readonly IQFeedDataQueueHandler _iQFeedDataQueueHandler = new IQFeedDataQueueHandler();
        private readonly IQFeedDataQueueUniverseProvider _iqFeedDataQueueUniverseProvider = new IQFeedDataQueueUniverseProvider();

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
            if (endUtc < startUtc)
            {
                throw new ArgumentException("The end date must be greater or equal than the start date.");
            }

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

            foreach (var slice in _iQFeedDataQueueHandler.GetHistory(new[] { historyRequest }, DateTimeZone.Utc))
            {
                yield return slice[symbol];
            }
        }

        /// <summary>
        /// Creates Lean Symbol
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns></returns>
        internal Symbol GetSymbol(string ticker)
        {
            // verify LEAN symbol mapping
            var leanSymbol = _iqFeedDataQueueUniverseProvider.GetLeanSymbol(ticker, SecurityType.Equity, "");
            return leanSymbol;
        }
    }
}
