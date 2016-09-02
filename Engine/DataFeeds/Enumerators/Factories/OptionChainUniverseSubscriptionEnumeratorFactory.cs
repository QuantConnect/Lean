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

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> for the <see cref="OptionChainUniverse"/>
    /// </summary>
    public class OptionChainUniverseSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        private readonly BaseDataSubscriptionEnumeratorFactory _factory;
        private readonly Func<SubscriptionRequest, IEnumerator<BaseData>, IEnumerator<BaseData>> _enumeratorConfigurator;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainUniverseSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="enumeratorConfigurator">Function used to configure the sub-enumerators before sync (fill-forward/filter/ect...)</param>
        public OptionChainUniverseSubscriptionEnumeratorFactory(Func<SubscriptionRequest, IEnumerator<BaseData>, IEnumerator<BaseData>> enumeratorConfigurator)
        {
            _enumeratorConfigurator = enumeratorConfigurator;
            _factory = new BaseDataSubscriptionEnumeratorFactory();
        }

        /// <summary>
        /// Creates an enumerator to read the specified request
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request)
        {
            var enumerators = GetSubscriptionConfigurations(request)
                .Select(c => new SubscriptionRequest(request, configuration: c))
                .Select(sr => _enumeratorConfigurator(request, _factory.CreateEnumerator(sr))
                );

            var sync = new SynchronizingEnumerator(enumerators);
            return new OptionChainUniverseDataCollectionAggregatorEnumerator(sync, request.Security.Symbol);
        }

        private IEnumerable<SubscriptionDataConfig> GetSubscriptionConfigurations(SubscriptionRequest request)
        {
            // canonical also needs underlying price data
            var config = request.Configuration;
            var underlying = Symbol.Create(config.Symbol.ID.Symbol, SecurityType.Equity, config.Market);
            var resolution = config.Resolution == Resolution.Tick ? Resolution.Second : config.Resolution;
            return new[]
            {
                // rewrite the primary to be non-tick and fill forward
                new SubscriptionDataConfig(config, resolution: resolution, fillForward: true),
                // add underlying trade data
                new SubscriptionDataConfig(config, resolution: resolution, fillForward: true, symbol: underlying, objectType: typeof (TradeBar), tickType: TickType.Trade),
            };
        }
    }
}