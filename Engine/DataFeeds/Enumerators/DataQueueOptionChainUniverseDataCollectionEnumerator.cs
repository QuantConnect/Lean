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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Data.Auxiliary;
using System.Collections;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerates live options symbol universe data into <see cref="OptionChainUniverseDataCollection"/> instances
    /// </summary>
    public class DataQueueOptionChainUniverseDataCollectionEnumerator : IEnumerator<OptionChainUniverseDataCollection>
    {
        private readonly SubscriptionRequest _subscriptionRequest;
        private readonly IDataQueueUniverseProvider _universeProvider;
        private readonly ITimeProvider _timeProvider;
        private bool _needNewCurrent;
        private DateTime _lastEmitTime;
        private OptionChainUniverseDataCollection _currentData;

        /// <summary>
        /// Gets the enumerator for the underlying asset
        /// </summary>
        public IEnumerator<BaseData> Underlying { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataQueueOptionChainUniverseDataCollectionEnumerator"/> class.
        /// </summary>
        /// <param name="subscriptionRequest">The subscription request to be used</param>
        /// <param name="underlying">Underlying enumerator</param>
        /// <param name="universeProvider">Symbol universe provider of the data queue</param>
        /// <param name="timeProvider">The time provider to be used</param>
        public DataQueueOptionChainUniverseDataCollectionEnumerator(
            SubscriptionRequest subscriptionRequest,
            IEnumerator<BaseData> underlying,
            IDataQueueUniverseProvider universeProvider,
            ITimeProvider timeProvider)
        {
            _subscriptionRequest = subscriptionRequest;
            Underlying = underlying;
            _universeProvider = universeProvider;
            _timeProvider = timeProvider;

            _needNewCurrent = true;
        }

        /// <summary>
        /// Returns current option chain enumerator position
        /// </summary>
        public OptionChainUniverseDataCollection Current { get; private set; }

        /// <summary>
        /// Returns current option chain enumerator position
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Underlying.Dispose();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            Underlying.MoveNext();

            if (Underlying.Current == null)
            {
                Current = null;
                return true;
            }

            if (!_needNewCurrent)
            {
                // refresh on date change (in exchange time zone)
                _needNewCurrent = _timeProvider.GetUtcNow().ConvertFromUtc(_subscriptionRequest.Configuration.ExchangeTimeZone).Date != _lastEmitTime.Date;
            }

            if (_needNewCurrent)
            {
                var localTime = _timeProvider.GetUtcNow()
                    .RoundDown(_subscriptionRequest.Configuration.Increment)
                    .ConvertFromUtc(_subscriptionRequest.Configuration.ExchangeTimeZone);

                // loading the list of futures contracts and converting them into zip entries
                var symbols = _universeProvider.LookupSymbols(_subscriptionRequest.Security.Symbol, false);
                var zipEntries = symbols.Select(x => new ZipEntryName { Time = localTime, Symbol = x } as BaseData).ToList();
                _currentData = new OptionChainUniverseDataCollection
                {
                    Symbol = _subscriptionRequest.Security.Symbol,
                    Underlying = Underlying.Current,
                    Data = zipEntries,
                    Time = localTime,
                    EndTime = localTime
                };

                _lastEmitTime = localTime;

                Current = _currentData;
                _needNewCurrent = false;
            }
            else
            {
                if (Current == null)
                {
                    Current = _currentData;
                }

                Current.Underlying = Underlying.Current;
                Current.Time = Underlying.Current.EndTime;
                Current.EndTime = Underlying.Current.EndTime;
            }

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            Underlying.Reset();
            _needNewCurrent = true;
        }
    }
}
