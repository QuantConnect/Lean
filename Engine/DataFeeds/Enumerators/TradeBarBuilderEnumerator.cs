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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Aggregates ticks into trade bars ready to be time synced
    /// </summary>
    public class TradeBarBuilderEnumerator : IEnumerator<BaseData>
    {
        private readonly TimeSpan _barSize;
        private readonly DateTimeZone _timeZone;
        private readonly ITimeProvider _timeProvider;
        private readonly ConcurrentQueue<TradeBar> _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TradeBarBuilderEnumerator"/> class
        /// </summary>
        /// <param name="barSize">The trade bar size to produce</param>
        /// <param name="timeZone">The time zone the raw data is time stamped in</param>
        /// <param name="timeProvider">The time provider instance used to determine when bars are completed and
        /// can be emitted</param>
        public TradeBarBuilderEnumerator(TimeSpan barSize, DateTimeZone timeZone, ITimeProvider timeProvider)
        {
            _barSize = barSize;
            _timeZone = timeZone;
            _timeProvider = timeProvider;
            _queue = new ConcurrentQueue<TradeBar>();
        }
        /// <summary>
        /// Pushes the tick into this enumerator. This tick will be aggregated into a bar
        /// and emitted after the alotted time has passed
        /// </summary>
        /// <param name="data">The new data to be aggregated</param>
        public void ProcessData(BaseData data)
        {
            TradeBar working;
            var tick = data as Tick;
            var qty = tick == null ? 0 : tick.Quantity;
            if (!_queue.TryPeek(out working))
            {
                // the consumer took the working bar, or time ticked over into next bar
                var marketPrice = data.Value;
                var currentLocalTime = _timeProvider.GetUtcNow().ConvertFromUtc(_timeZone);
                var barStartTime = currentLocalTime.RoundDown(_barSize);
                working = new TradeBar(barStartTime, data.Symbol, marketPrice, marketPrice, marketPrice, marketPrice, qty, _barSize);
                _queue.Enqueue(working);
            }
            else
            {
                // we're still within this bar size's time
                var bidPrice = tick == null ? data.Value : tick.BidPrice;
                var askPrice = tick == null ? data.Value : tick.AskPrice;
                var bidSize = tick == null ? 0m : tick.BidSize;
                var askSize = tick == null ? 0m : tick.AskSize;
                working.Update(data.Value, bidPrice, askPrice, qty, bidSize, askSize);
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            TradeBar working;

            // check if there's a bar there and if its time to pull it off (i.e, done aggregation)
            if (_queue.TryPeek(out working) && working.EndTime.ConvertToUtc(_timeZone) <= _timeProvider.GetUtcNow())
            {
                // working is good to go, set it to current
                Current = working;
                // remove working from the queue so we can start aggregating the next bar
                _queue.TryDequeue(out working);
            }
            else
            {
                Current = null;
            }

            // IEnumerator contract dictates that we return true unless we're actually
            // finished with the 'collection' and since this is live, we're never finished
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }
    }
}