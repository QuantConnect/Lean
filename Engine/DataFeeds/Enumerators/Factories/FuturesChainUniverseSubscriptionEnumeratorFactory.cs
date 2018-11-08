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

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> for the <see cref="FuturesChainUniverse"/> in backtesting
    /// </summary>
    public class FuturesChainUniverseSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly Func<SubscriptionRequest, IEnumerator<BaseData>, IEnumerator<BaseData>> _enumeratorConfigurator;
        private readonly bool _isLiveMode;

        private readonly IDataQueueUniverseProvider _symbolUniverse;
        private readonly ITimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChainUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="enumeratorConfigurator">Function used to configure the sub-enumerators before sync (fill-forward/filter/ect...)</param>
        public FuturesChainUniverseSubscriptionEnumeratorFactory(Func<SubscriptionRequest, IEnumerator<BaseData>, IEnumerator<BaseData>> enumeratorConfigurator)
        {
            _isLiveMode = false;
            _enumeratorConfigurator = enumeratorConfigurator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuturesChainUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="symbolUniverse">Symbol universe provider of the data queue</param>
        /// <param name="timeProvider">The time provider to be used</param>
        public FuturesChainUniverseSubscriptionEnumeratorFactory(IDataQueueUniverseProvider symbolUniverse, ITimeProvider timeProvider)
        {
            _isLiveMode = true;
            _symbolUniverse = symbolUniverse;
            _timeProvider = timeProvider;
            _enumeratorConfigurator = (sr, input) => input;
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
                var subscriptionConfiguration = GetSubscriptionConfigurations(request).First();
                var subscriptionRequest = new SubscriptionRequest(request, configuration: subscriptionConfiguration);

                return new DataQueueFuturesChainUniverseDataCollectionEnumerator(subscriptionRequest, _symbolUniverse, _timeProvider);
            }
            else
            {
                var factory = new BaseDataSubscriptionEnumeratorFactory(_isLiveMode);

                var enumerators = GetSubscriptionConfigurations(request)
                    .Select(c => new SubscriptionRequest(request, configuration: c))
                    .Select(sr => _enumeratorConfigurator(request, factory.CreateEnumerator(sr, dataProvider)));

                var sync = new SynchronizingEnumerator(enumerators);
                return new FuturesChainUniverseDataCollectionAggregatorEnumerator(sync, request.Security.Symbol);
            }
        }

        private static IEnumerable<SubscriptionDataConfig> GetSubscriptionConfigurations(SubscriptionRequest request)
        {
            var config = request.Configuration;
            var resolution = config.Resolution;

            var configurations = new List<SubscriptionDataConfig>
            {
                // rewrite the primary to be fill forward
                new SubscriptionDataConfig(config, resolution: resolution, fillForward: true)
            };

            return configurations;
        }
    }
}