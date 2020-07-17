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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> for the <see cref="OptionChainUniverse"/>
    /// </summary>
    public class OptionChainUniverseSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly Func<SubscriptionRequest, IEnumerator<BaseData>> _enumeratorConfigurator;
        private readonly bool _isLiveMode;

        private readonly IDataQueueUniverseProvider _symbolUniverse;
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="enumeratorConfigurator">Function used to configure the sub-enumerators before sync (fill-forward/filter/ect...)</param>
        public OptionChainUniverseSubscriptionEnumeratorFactory(Func<SubscriptionRequest, IEnumerator<BaseData>> enumeratorConfigurator)
        {
            _isLiveMode = false;
            _enumeratorConfigurator = enumeratorConfigurator;

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="enumeratorConfigurator">Function used to configure the sub-enumerators before sync (fill-forward/filter/ect...)</param>
        /// <param name="symbolUniverse">Symbol universe provider of the data queue</param>
        /// <param name="timeProvider">The time provider instance used to determine when bars are completed and can be emitted</param>
        public OptionChainUniverseSubscriptionEnumeratorFactory(Func<SubscriptionRequest, IEnumerator<BaseData>> enumeratorConfigurator,
                                                                IDataQueueUniverseProvider symbolUniverse, ITimeProvider timeProvider)
        {
            _isLiveMode = true;
            _symbolUniverse = symbolUniverse;
            _timeProvider = timeProvider;
            _enumeratorConfigurator = enumeratorConfigurator;
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, IDataProvider dataProvider)
        {
            if (_isLiveMode)
            {
                // configuring the enumerator
                var subscriptionConfiguration = GetSubscriptionConfigurations(request).First();
                var subscriptionRequest = new SubscriptionRequest(request, configuration: subscriptionConfiguration);
                var configuredEnumerator = _enumeratorConfigurator(subscriptionRequest);

                return new DataQueueOptionChainUniverseDataCollectionEnumerator(subscriptionRequest, configuredEnumerator, _symbolUniverse, _timeProvider);
            }
            else
            {
                var enumerators = GetSubscriptionConfigurations(request)
                    .Select(c => new SubscriptionRequest(request, configuration: c))
                    .Select(sr => _enumeratorConfigurator(sr));

                var sync = new SynchronizingEnumerator(enumerators);
                return new OptionChainUniverseDataCollectionEnumerator(sync, request.Security.Symbol);
            }
        }

        private IEnumerable<SubscriptionDataConfig> GetSubscriptionConfigurations(SubscriptionRequest request)
        {
            // canonical also needs underlying price data
            var config = request.Configuration;
            var underlying = config.Symbol.Underlying;

            // Making sure data is non-tick
            var resolution = config.Resolution == Resolution.Tick ? Resolution.Second : config.Resolution;

            var configurations = new List<SubscriptionDataConfig>
            {
                // add underlying trade data
                new SubscriptionDataConfig(config, resolution: resolution, fillForward: true, symbol: underlying, objectType: typeof (TradeBar), tickType: TickType.Trade),
            };

            if (!_isLiveMode)
            {
                // rewrite the primary to be fill forward
                configurations.Add(new SubscriptionDataConfig(config, resolution: resolution, fillForward: true));
            }

            return configurations;
        }
    }
}
