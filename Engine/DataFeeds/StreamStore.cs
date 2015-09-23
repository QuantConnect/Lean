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
 *
*/

using System;
using System.Collections.Concurrent;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// The stream store accepts data updates from the data feed and aggregates it into bars.
    /// Custom data is not aggregated, just saved for when TriggerArchive is called.
    /// </summary>
    public class StreamStore
    {
        private BaseData _previous;

        private readonly Security _security;
        private readonly TimeSpan _increment;
        private readonly SubscriptionDataConfig _config;
        private readonly ConcurrentQueue<BaseData> _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamStore"/> class
        /// </summary>
        /// <param name="config">The subscripton's configuration</param>
        /// <param name="security">The security object, used for exchange hours</param>
        public StreamStore(SubscriptionDataConfig config, Security security)
        {
            _security = security;
            _config = config;
            _increment = config.Increment;
            _queue = new ConcurrentQueue<BaseData>();
        }

        /// <summary>
        /// Updates the current working bar or creates a new working bar if TriggerArchive has been called
        /// </summary>
        /// <remarks>
        /// This method assumes only one thread will be using this method. It is intended to
        /// be consumed by the live trading data feed data tasks (live/custom)
        /// </remarks>
        /// <param name="tick">The new data to aggregate</param>
        public void Update(Tick tick)
        {
            if (!IsMarketOpen(tick)) return;

            // get the current working bar and update it
            BaseData working;
            if (!_queue.TryPeek(out working))
            {
                // the consumer took the bar, create a new one
                working = CreateNewTradeBar(tick.LastPrice, tick.Quantity);
                _queue.Enqueue(working);
            }
            working.Update(tick.LastPrice, tick.BidPrice, tick.AskPrice, tick.Quantity);
        }

        /// <summary>
        /// Enqueues the new data directly for the real time sync thread to take it via TriggerArchive
        /// </summary>
        /// <remarks>
        /// This method assumes only one thread will be using this method. It is intended to
        /// be consumed by the live trading data feed data tasks (live/custom)
        /// </remarks>
        /// <param name="baseData">The new custom data</param>
        public void Update(BaseData baseData)
        {
            if (!IsMarketOpen(baseData)) return;

            // custom data doesn't get aggregated, just push it into the queue
            _queue.Enqueue(baseData);
        }

        /// <summary>
        /// Dequeues the current working bar
        /// </summary>
        /// <param name="utcTriggerTime">The current trigger time in UTC</param>
        /// <returns>The base data instance, or null, if no data is to be emitted</returns>
        public BaseData TriggerArchive(DateTime utcTriggerTime)
        {
            BaseData bar;
            if (!_queue.TryDequeue(out bar))
            {
                // if a bar wasn't ready, check for fill forward
                if (_previous != null && _config.FillDataForward)
                {
                    // exchanges hours are in local time, so convert to local before checking if exchange is open
                    var localTriggerTime = utcTriggerTime.ConvertFromUtc(_config.TimeZone);

                    // only perform fill forward behavior if the exchange is considered open
                    var barStartTime = localTriggerTime - _increment;
                    if (_security.Exchange.IsOpenDuringBar(barStartTime, localTriggerTime, _config.ExtendedMarketHours))
                    {
                        bar = _previous.Clone(true);
                        bar.Time = barStartTime.ExchangeRoundDown(_increment, _security.Exchange.Hours, _security.IsExtendedMarketHours);
                    }
                }
            }

            // we don't have data, so just return null
            if (bar == null) return null;

            // reset the previous bar for fill forward
            _previous = bar.Clone();

            return bar;
        }

        private TradeBar CreateNewTradeBar(decimal marketPrice, long volume)
        {
            return new TradeBar(ComputeBarStartTime(), _config.Symbol, marketPrice, marketPrice, marketPrice, marketPrice, volume, _increment);
        }

        /// <summary>
        /// Computes the start time of the bar this data belongs in
        /// </summary>
        private DateTime ComputeBarStartTime()
        {
            return DateTime.UtcNow.RoundDown(_increment).ConvertFromUtc(_config.TimeZone);
        }

        private bool IsMarketOpen(BaseData tick)
        {
            return _security.Exchange.IsOpenDuringBar(tick.Time, tick.EndTime, _config.ExtendedMarketHours);
        }
    }
}
