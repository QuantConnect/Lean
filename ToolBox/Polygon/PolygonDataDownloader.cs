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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.Polygon
{
    /// <summary>
    /// Data downloader class for pulling data from Polygon.io
    /// </summary>
    public class PolygonDataDownloader : IDataDownloader, IDisposable
    {
        private readonly PolygonDataQueueHandler _historyProvider = new PolygonDataQueueHandler(false);

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
            if (resolution != Resolution.Minute &&
                resolution != Resolution.Hour &&
                resolution != Resolution.Daily)
            {
                throw new NotSupportedException($"Resolution not supported: {resolution}");
            }

            if (symbol.SecurityType != SecurityType.Equity &&
                symbol.SecurityType != SecurityType.Forex && 
                symbol.SecurityType != SecurityType.Crypto)
            {
                throw new NotSupportedException($"Security type not supported: {symbol.SecurityType}");
            }

            if (endUtc < startUtc)
            {
                throw new ArgumentException("The end date must be greater or equal than the start date.");
            }

            var historyRequests = new[]
            {
                new HistoryRequest(startUtc,
                    endUtc,
                    typeof(TradeBar),
                    symbol,
                    resolution,
                    SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                    TimeZones.NewYork,
                    resolution,
                    false,
                    false,
                    DataNormalizationMode.Adjusted,
                    TickType.Trade)
            };

            foreach (var slice in _historyProvider.GetHistory(historyRequests, TimeZones.NewYork))
            {
                yield return slice[symbol];
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _historyProvider.DisposeSafely();
        }
    }
}
