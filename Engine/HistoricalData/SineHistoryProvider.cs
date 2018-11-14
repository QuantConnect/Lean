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
using QuantConnect.Lean.Engine.DataFeeds;
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
    public class SineHistoryProvider : HistoryProviderBase
    {
        private readonly SecurityChanges _securityChanges = SecurityChanges.None;
        private readonly SecurityManager _securities;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => 0;

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
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var configsByDateTime = GetSubscriptionDataConfigByDateTime(requests);
            var count = configsByDateTime.Count;
            var i = 0;
            var timeSliceFactory = new TimeSliceFactory(sliceTimeZone);
            foreach (var kvp in configsByDateTime)
            {
                var utcDateTime = kvp.Key;
                var configs = kvp.Value;
                var last = Convert.ToDecimal(100 + 10 * Math.Sin(Math.PI * (360 - count + i) / 180.0));
                var high = last * 1.005m;
                var low = last / 1.005m;

                var packets = new List<DataFeedPacket>();

                foreach (var config in configs)
                {
                    Security security;
                    if (!_securities.TryGetValue(config.Symbol, out security))
                    {
                        continue;
                    }

                    var period = config.Resolution.ToTimeSpan();
                    var time = (utcDateTime - period).ConvertFromUtc(config.DataTimeZone);
                    var data = new TradeBar(time, config.Symbol, last, high, last, last, 1000, period);
                    security.SetMarketPrice(data);
                    packets.Add(new DataFeedPacket(security, config, new List<BaseData> { data }));
                }

                i++;
                yield return timeSliceFactory.Create(utcDateTime, packets, _securityChanges, new Dictionary<Universe, BaseDataCollection>()).Slice;
            }
        }

        private Dictionary<DateTime, List<SubscriptionDataConfig>> GetSubscriptionDataConfigByDateTime(
            IEnumerable<HistoryRequest> requests)
        {
            var dictionary = new Dictionary<DateTime, List<SubscriptionDataConfig>>();

            var barSize = requests.Select(x => x.Resolution.ToTimeSpan()).Min();
            var startUtc = requests.Min(x => x.StartTimeUtc);
            var endUtc = requests.Max(x => x.EndTimeUtc);

            for (var utcDateTime = startUtc; utcDateTime < endUtc; utcDateTime += barSize)
            {
                var subscriptionDataConfig = new List<SubscriptionDataConfig>();

                foreach (var request in requests)
                {
                    var exchange = request.ExchangeHours;
                    var extendedMarket = request.IncludeExtendedMarketHours;
                    var localDateTime = utcDateTime.ConvertFromUtc(exchange.TimeZone);
                    if (!exchange.IsOpen(localDateTime, extendedMarket))
                    {
                        continue;
                    }

                    var config = new SubscriptionDataConfig(request.DataType,
                        request.Symbol,
                        request.Resolution,
                        request.DataTimeZone,
                        request.ExchangeHours.TimeZone,
                        request.FillForwardResolution.HasValue,
                        request.IncludeExtendedMarketHours,
                        false,
                        request.IsCustomData,
                        request.TickType,
                        true,
                        request.DataNormalizationMode
                    );

                    subscriptionDataConfig.Add(config);
                }

                if (subscriptionDataConfig.Count > 0)
                {
                    dictionary.Add(utcDateTime.Add(barSize), subscriptionDataConfig);
                }
            }

            return dictionary;
        }
    }
}