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
using QuantConnect.Data;
using System.Collections;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// A live trading delisting event provider which uses a <see cref="DelistingEventProvider"/> internally to emit events
    /// based on the current frontier time
    /// </summary>
    /// <remarks>We create the events dynamically so that we can set the current security price in the event</remarks>
    public class LiveDelistingEventProviderEnumerator : IEnumerator<BaseData>
    {
        private DateTime _lastTime;
        private readonly ITimeProvider _timeProvider;
        private readonly Queue<BaseData> _dataToEmit;
        private readonly SecurityCache _securityCache;
        private readonly SubscriptionDataConfig _dataConfig;
        private readonly DelistingEventProvider _delistingEventProvider;

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
        /// Creates a new instance
        /// </summary>
        private LiveDelistingEventProviderEnumerator(ITimeProvider timeProvider, SubscriptionDataConfig dataConfig,
            SecurityCache securityCache, DelistingEventProvider delistingEventProvider, MapFile mapFile)
        {
            _dataConfig = dataConfig;
            _timeProvider = timeProvider;
            _securityCache = securityCache;
            _dataToEmit = new Queue<BaseData>();
            _delistingEventProvider = delistingEventProvider;
            _delistingEventProvider.Initialize(dataConfig, null, mapFile, DateTime.UtcNow);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns> true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public bool MoveNext()
        {
            var currentDate = _timeProvider.GetUtcNow().ConvertFromUtc(_dataConfig.ExchangeTimeZone).Date;
            if (currentDate != _lastTime)
            {
                // when the date changes for the security we trigger a new tradable date event
                var newDayEvent = new NewTradableDateEventArgs(currentDate, _securityCache.GetData(), _dataConfig.Symbol, null);
                foreach (var delistingEvent in _delistingEventProvider.GetEvents(newDayEvent))
                {
                    _dataToEmit.Enqueue(delistingEvent);
                }

                // update last time
                _lastTime = currentDate;
            }

            if (_dataToEmit.Count > 0)
            {
                // emit event if any
                Current = _dataToEmit.Dequeue();
                return true;
            }

            Current = null;
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public void Reset()
        {
            _dataToEmit.Clear();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var liveProvider = _delistingEventProvider as LiveDataBasedDelistingEventProvider;
            liveProvider.DisposeSafely();
        }

        /// <summary>
        /// Helper method to create a new instance.
        /// Knows which security types should create one and determines the appropriate delisting event provider to use
        /// </summary>
        public static bool TryCreate(SubscriptionDataConfig dataConfig, ITimeProvider timeProvider, IDataQueueHandler dataQueueHandler,
            SecurityCache securityCache, IMapFileProvider mapFileProvider, out IEnumerator<BaseData> enumerator)
        {
            enumerator = null;
            var securityType = dataConfig.SecurityType;
            if (securityType == SecurityType.FutureOption || securityType == SecurityType.Future
                || securityType == SecurityType.Option || securityType == SecurityType.Equity)
            {
                var delistingEventProvider = new DelistingEventProvider();
                MapFile mapFile = null;
                if (securityType == SecurityType.Equity)
                {
                    delistingEventProvider = new LiveDataBasedDelistingEventProvider(dataConfig, dataQueueHandler);
                    mapFile = mapFileProvider.Get(dataConfig.Symbol.ID.Market).ResolveMapFile(dataConfig.Symbol, dataConfig.Type);
                }
                enumerator = new LiveDelistingEventProviderEnumerator(timeProvider, dataConfig, securityCache, delistingEventProvider, mapFile);
                return true;
            }
            return false;
        }
    }
}
