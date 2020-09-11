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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> to emit
    /// ticks based on <see cref="UserDefinedUniverse.GetTriggerTimes"/>, allowing universe
    /// selection to fire at planned times.
    /// </summary>
    public class TimeTriggeredUniverseSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly ITimeProvider _timeProvider;
        private readonly ITimeTriggeredUniverse _universe;
        private readonly MarketHoursDatabase _marketHoursDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeTriggeredUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="universe">The user defined universe</param>
        /// <param name="marketHoursDatabase">The market hours database</param>
        /// <param name="timeProvider">The time provider</param>
        public TimeTriggeredUniverseSubscriptionEnumeratorFactory(ITimeTriggeredUniverse universe, MarketHoursDatabase marketHoursDatabase, ITimeProvider timeProvider)
        {
            _universe = universe;
            _timeProvider = timeProvider;
            _marketHoursDatabase = marketHoursDatabase;
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, IDataProvider dataProvider)
        {
            var enumerator = (IEnumerator<BaseData>) _universe.GetTriggerTimes(request.StartTimeUtc, request.EndTimeUtc, _marketHoursDatabase)
                .Select(x => new Tick { Time = x, Symbol = request.Configuration.Symbol })
                .GetEnumerator();

            var universe = request.Universe as UserDefinedUniverse;
            if (universe != null)
            {
                enumerator = new InjectionEnumerator(enumerator);

                // Trigger universe selection when security added/removed after Initialize
                universe.CollectionChanged += (sender, args) =>
                {
                    var items =
                        args.Action == NotifyCollectionChangedAction.Add ? args.NewItems :
                        args.Action == NotifyCollectionChangedAction.Remove ? args.OldItems : null;

                    var time = _timeProvider.GetUtcNow();
                    if (items == null || time == DateTime.MinValue) return;

                    var symbol = items.OfType<Symbol>().FirstOrDefault();

                    if(symbol == null) return;

                    // the data point time should always be in exchange timezone
                    time = time.ConvertFromUtc(request.Configuration.ExchangeTimeZone);

                    var collection = new BaseDataCollection(time, symbol);

                    ((InjectionEnumerator) enumerator).InjectDataPoint(collection);
                };
            }

            return enumerator;
        }

        private class InjectionEnumerator : IEnumerator<BaseData>
        {
            private volatile bool _wasInjected;
            private readonly IEnumerator<BaseData> _underlyingEnumerator;

            public BaseData Current { get; private set; }

            object IEnumerator.Current => Current;

            public InjectionEnumerator(IEnumerator<BaseData> underlyingEnumerator)
            {
                _underlyingEnumerator = underlyingEnumerator;
            }

            public void InjectDataPoint(BaseData baseData)
            {
                // we use a lock because the main algorithm thread is the one injecting and the base exchange is the thread pulling MoveNext()
                lock (_underlyingEnumerator)
                {
                    _wasInjected = true;
                    Current = baseData;
                }
            }

            public void Dispose()
            {
                _underlyingEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                lock (_underlyingEnumerator)
                {
                    if (_wasInjected)
                    {
                        _wasInjected = false;
                        return true;
                    }
                    _underlyingEnumerator.MoveNext();
                    Current = _underlyingEnumerator.Current;
                    return true;
                }
            }

            public void Reset()
            {
                _underlyingEnumerator.Reset();
            }
        }
    }
}