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
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Represents an enumerator capable of synchronizing live equity data enumerators in time.
    /// This assumes that all enumerators have data time stamped in the same time zone.
    /// </summary>
    public class LiveEquityDataSynchronizingEnumerator : IEnumerator<BaseData>
    {
        private readonly ITimeProvider _timeProvider;
        private readonly DateTimeZone _exchangeTimeZone;
        private readonly List<IEnumerator<BaseData>> _auxDataEnumerators;
        private readonly IEnumerator<BaseData> _tradeBarAggregator;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveEquityDataSynchronizingEnumerator"/> class
        /// </summary>
        /// <param name="timeProvider">The source of time used to gauge when this enumerator should emit extra bars when null data is returned from the source enumerator</param>
        /// <param name="exchangeTimeZone">The time zone the raw data is time stamped in</param>
        /// <param name="tradeBarAggregator">The trade bar aggregator enumerator</param>
        /// <param name="auxDataEnumerators">The auxiliary data enumerators</param>
        public LiveEquityDataSynchronizingEnumerator(ITimeProvider timeProvider, DateTimeZone exchangeTimeZone, IEnumerator<BaseData> tradeBarAggregator, params IEnumerator<BaseData>[] auxDataEnumerators)
        {
            _timeProvider = timeProvider;
            _exchangeTimeZone = exchangeTimeZone;
            _auxDataEnumerators = auxDataEnumerators.ToList();
            _tradeBarAggregator = tradeBarAggregator;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns> true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            // use manual time provider from LiveTradingDataFeed
            var frontierUtc = _timeProvider.GetUtcNow();

            // check if any enumerator is ready to emit
            if (DataPointEmitted(frontierUtc))
                return true;

            // advance enumerators with no current data
            for (var i = 0; i < _auxDataEnumerators.Count; i++)
            {
                if (_auxDataEnumerators[i].Current == null)
                {
                    _auxDataEnumerators[i].MoveNext();
                }
            }
            if (_tradeBarAggregator.Current == null) _tradeBarAggregator.MoveNext();

            // check if any enumerator is ready to emit
            if (DataPointEmitted(frontierUtc))
                return true;

            Current = null;

            // IEnumerator contract dictates that we return true unless we're actually
            // finished with the 'collection' and since this is live, we're never finished
            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public void Reset()
        {
            foreach (var auxDataEnumerator in _auxDataEnumerators)
            {
                auxDataEnumerator.Reset();
            }
            _tradeBarAggregator.Reset();
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public BaseData Current { get; private set; }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>The current element in the collection.</returns>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var auxDataEnumerator in _auxDataEnumerators)
            {
                auxDataEnumerator.DisposeSafely();
            }
            _tradeBarAggregator.DisposeSafely();
        }

        private bool DataPointEmitted(DateTime frontierUtc)
        {
            // we get the aux enumerator that has the smallest endTime if any
            IEnumerator<BaseData> auxDataEnumerator = null;
            for (var i = 0; i < _auxDataEnumerators.Count; i++)
            {
                var currentEnum = _auxDataEnumerators[i];
                if (currentEnum.Current != null)
                {
                    if (auxDataEnumerator == null)
                    {
                        auxDataEnumerator = currentEnum;
                    }
                    else
                    {
                        auxDataEnumerator = auxDataEnumerator.Current.EndTime > currentEnum.Current.EndTime ? currentEnum : auxDataEnumerator;
                    }
                }
            }

            // check if any enumerator is ready to emit
            if (auxDataEnumerator?.Current != null && _tradeBarAggregator.Current != null)
            {
                var auxDataEndTime = auxDataEnumerator.Current.EndTime.ConvertToUtc(_exchangeTimeZone);
                var tradeBarEndTime = _tradeBarAggregator.Current.EndTime.ConvertToUtc(_exchangeTimeZone);
                if (auxDataEndTime < tradeBarEndTime)
                {
                    if (auxDataEndTime <= frontierUtc)
                    {
                        Current = auxDataEnumerator.Current;
                        auxDataEnumerator.MoveNext();
                        return true;
                    }
                }
                else
                {
                    if (tradeBarEndTime <= frontierUtc)
                    {
                        Current = _tradeBarAggregator.Current;
                        _tradeBarAggregator.MoveNext();
                        return true;
                    }
                }
            }
            else if (auxDataEnumerator?.Current != null)
            {
                var auxDataEndTime = auxDataEnumerator.Current.EndTime.ConvertToUtc(_exchangeTimeZone);
                if (auxDataEndTime <= frontierUtc)
                {
                    Current = auxDataEnumerator.Current;
                    auxDataEnumerator.MoveNext();
                    return true;
                }
            }
            else if (_tradeBarAggregator.Current != null)
            {
                var tradeBarEndTime = _tradeBarAggregator.Current.EndTime.ConvertToUtc(_exchangeTimeZone);
                if (tradeBarEndTime <= frontierUtc)
                {
                    Current = _tradeBarAggregator.Current;
                    _tradeBarAggregator.MoveNext();
                    return true;
                }
            }

            return false;
        }
    }
}
