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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Auxiliary data enumerator that will trigger new tradable dates event accordingly
    /// </summary>
    public class LiveAuxiliaryDataEnumerator : AuxiliaryDataEnumerator
    {
        private DateTime _lastTime;
        private ITimeProvider _timeProvider;
        private SecurityCache _securityCache;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">The factor file provider to use</param>
        /// <param name="mapFileProvider">The <see cref="MapFile"/> provider to use</param>
        /// <param name="tradableDateEventProviders">The tradable dates event providers</param>
        /// <param name="startTime">Start date for the data request</param>
        /// <param name="timeProvider">The time provider to use</param>
        /// <param name="securityCache">The security cache</param>
        public LiveAuxiliaryDataEnumerator(SubscriptionDataConfig config, IFactorFileProvider factorFileProvider,
            IMapFileProvider mapFileProvider, ITradableDateEventProvider[] tradableDateEventProviders,
            DateTime startTime,
            ITimeProvider timeProvider,
            SecurityCache securityCache)
            // tradableDayNotifier: null -> we are going to trigger the new tradables events for the base implementation
            : base(config, factorFileProvider, mapFileProvider, tradableDateEventProviders, tradableDayNotifier:null, startTime)
        {
            _securityCache = securityCache;
            _timeProvider = timeProvider;

            // initialize providers right away so mapping happens before we subscribe
            Initialize();
        }

        public override bool MoveNext()
        {
            var currentDate = _timeProvider.GetUtcNow().ConvertFromUtc(Config.ExchangeTimeZone).Add(-Time.LiveAuxiliaryDataOffset).Date;
            if (currentDate != _lastTime)
            {
                // when the date changes for the security we trigger a new tradable date event
                var newDayEvent = new NewTradableDateEventArgs(currentDate, _securityCache.GetData(), Config.Symbol, null);

                NewTradableDate(this, newDayEvent);
                // update last time
                _lastTime = currentDate;
            }

            return base.MoveNext();
        }

        /// <summary>
        /// Helper method to create a new instance.
        /// Knows which security types should create one and determines the appropriate delisting event provider to use
        /// </summary>
        public static bool TryCreate(SubscriptionDataConfig dataConfig, ITimeProvider timeProvider,
            SecurityCache securityCache, IMapFileProvider mapFileProvider, IFactorFileProvider fileProvider, DateTime startTime,
            out IEnumerator<BaseData> enumerator)
        {
            enumerator = null;
            var securityType = dataConfig.SecurityType;
            if (securityType.IsOption() || securityType == SecurityType.Future || securityType == SecurityType.Equity)
            {
                var providers = new List<ITradableDateEventProvider>
                {
                    securityType == SecurityType.Equity
                        ? new LiveDelistingEventProvider()
                        : new DelistingEventProvider()
                };

                if (dataConfig.TickerShouldBeMapped())
                {
                    providers.Add(new LiveMappingEventProvider());
                }

                if (dataConfig.EmitSplitsAndDividends())
                {
                    providers.Add(new LiveDividendEventProvider());
                    providers.Add(new LiveSplitEventProvider());
                }

                enumerator = new LiveAuxiliaryDataEnumerator(dataConfig, fileProvider, mapFileProvider,
                    providers.ToArray(), startTime, timeProvider, securityCache);
            }
            return enumerator != null;
        }
    }
}
