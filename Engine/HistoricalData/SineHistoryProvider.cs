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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Packets;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using HistoryRequest = QuantConnect.Data.HistoryRequest;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Implements a History provider that always return a IEnumerable of Slice with prices following a sine function
    /// </summary>
    public class SineHistoryProvider : IHistoryProvider
    {
        private CashBook _cashBook = new CashBook();
        private SecurityChanges _securityChanges = SecurityChanges.None;
        private SecurityManager _securities;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public int DataPointCount => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SineHistoryProvider"/> class
        /// </summary>
        /// <param name="securities">Collection of securities that a history request can return</param>
        public SineHistoryProvider(SecurityManager securities)
        {
            _securities = securities;
        }

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="job">The job</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <param name="dataCacheProvider">Provider used to cache history data files</param>
        /// <param name="mapFileProvider">Provider used to get a map file resolver to handle equity mapping</param>
        /// <param name="factorFileProvider">Provider used to get factor files to handle equity price scaling</param>
        /// <param name="statusUpdate">Function used to send status updates</param>
        public void Initialize(AlgorithmNodePacket job, IDataProvider dataProvider, IDataCacheProvider dataCacheProvider, IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, Action<int> statusUpdate)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var securitiesByDateTime = GetSecuritiesByDateTime(requests);
            var count = securitiesByDateTime.Count;
            var i = 0;

            foreach (var kvp in securitiesByDateTime)
            {
                var utcDateTime = kvp.Key;
                var securities = kvp.Value;
                var last = Convert.ToDecimal(100 + 10 * Math.Sin(Math.PI * (360 - count + i) / 180.0));
                var high = last * 1.005m;
                var low = last / 1.005m;

                var packets = new List<DataFeedPacket>();

                foreach (var security in securities)
                {
                    var configuration = security.Subscriptions.FirstOrDefault(x => x.Resolution == security.Resolution);
                    var period = security.Resolution.ToTimeSpan();
                    var time = (utcDateTime - period).ConvertFromUtc(configuration.DataTimeZone);
                    var data = new TradeBar(time, security.Symbol, last, high, last, last, 1000, period);
                    security.SetMarketPrice(data);
                    packets.Add(new DataFeedPacket(security, configuration, new List<BaseData> { data }));
                }

                i++;
                yield return TimeSlice.Create(utcDateTime, sliceTimeZone, _cashBook, packets, _securityChanges).Slice;
            }
        }

        private Dictionary<DateTime, List<Security>> GetSecuritiesByDateTime(IEnumerable<HistoryRequest> requests)
        {
            var dictionary = new Dictionary<DateTime, List<Security>>();

            var barSize = requests.Select(x => x.Resolution.ToTimeSpan()).Min();
            var startUtc = requests.Min(x => x.StartTimeUtc);
            var endUtc = requests.Max(x => x.EndTimeUtc);
            
            for (var utcDateTime = startUtc; utcDateTime < endUtc; utcDateTime += barSize)
            {
                var securities = new List<Security>();

                foreach (var request in requests)
                {
                    Security security;
                    if (!_securities.TryGetValue(request.Symbol, out security))
                    {
                        continue;
                    }

                    var exchange = security.Exchange.Hours;
                    var extendedMarket = security.IsExtendedMarketHours;
                    var localDateTime = utcDateTime.ConvertFromUtc(exchange.TimeZone);
                    if (!exchange.IsOpen(localDateTime, extendedMarket))
                    {
                        continue;
                    }

                    var configuration = security.Subscriptions.FirstOrDefault(x => x.Resolution == request.Resolution);
                    if (configuration == null)
                    {
                        continue;
                    }

                    securities.Add(security);
                }

                if (securities.Count > 0)
                {
                    dictionary.Add(utcDateTime.Add(barSize), securities);
                }
            }

            return dictionary;
        }
    }
}