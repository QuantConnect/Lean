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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds.Queues;

namespace QuantConnect.Lean.Engine.HistoricalData
{
    /// <summary>
    /// Provides FAKE implementation of <see cref="IHistoryProvider"/> used for testing. <see cref="FakeDataQueue"/>
    /// </summary>
    public class FakeHistoryProvider : HistoryProviderBase
    {
        private int _historyCount;

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _historyCount;

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
            var single = requests.FirstOrDefault();
            if (single == null)
            {
                yield break;
            }

            var currentLocalTime = single.StartTimeLocal;
            while (currentLocalTime < single.EndTimeLocal)
            {
                if (single.ExchangeHours.IsOpen(currentLocalTime, single.IncludeExtendedMarketHours))
                {
                    _historyCount++;

                    BaseData data;
                    if (single.DataType == typeof(TradeBar))
                    {
                        data = new TradeBar
                        {
                            Symbol = single.Symbol,
                            Time = currentLocalTime,
                            Open = _historyCount,
                            Low = _historyCount,
                            High = _historyCount,
                            Close = _historyCount,
                            Volume = _historyCount,
                            Period = single.Resolution.ToTimeSpan()
                        };
                    }
                    else if (single.DataType == typeof(QuoteBar))
                    {
                        data = new QuoteBar
                        {
                            Symbol = single.Symbol,
                            Time = currentLocalTime,
                            Ask = new Bar(_historyCount, _historyCount, _historyCount, _historyCount),
                            Bid = new Bar(_historyCount, _historyCount, _historyCount, _historyCount),
                            Period = single.Resolution.ToTimeSpan()
                        };
                    }
                    else
                    {
                        yield break;
                    }

                    yield return new Slice(data.EndTime, new BaseData[] { data }, data.EndTime.ConvertFromUtc(single.ExchangeHours.TimeZone));
                }

                currentLocalTime = currentLocalTime.Add(single.Resolution.ToTimeSpan());
            }
        }
    }
}
