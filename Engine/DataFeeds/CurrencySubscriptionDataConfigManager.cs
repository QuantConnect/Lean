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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Helper class to keep track of required internal currency <see cref="SubscriptionDataConfig"/>.
    /// This class is used by the <see cref="UniverseSelection"/>
    /// </summary>
    public class CurrencySubscriptionDataConfigManager
    {
        private readonly HashSet<SubscriptionDataConfig> _toBeAddedCurrencySubscriptionDataConfigs;
        private readonly HashSet<SubscriptionDataConfig> _addedCurrencySubscriptionDataConfigs;
        private bool _ensureCurrencyDataFeeds;
        private bool _pendingSubscriptionDataConfigs;
        private readonly CashBook _cashBook;
        private readonly Resolution _defaultResolution;
        private readonly SecurityManager _securityManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly ISecurityService _securityService;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="cashBook">The cash book instance</param>
        /// <param name="securityManager">The SecurityManager, required by the cash book for creating new securities</param>
        /// <param name="subscriptionManager">The SubscriptionManager, required by the cash book for creating new subscription data configs</param>
        /// <param name="securityService">The SecurityService, required by the cash book for creating new securities</param>
        /// <param name="defaultResolution">The default resolution to use for the internal subscriptions</param>
        public CurrencySubscriptionDataConfigManager(CashBook cashBook,
            SecurityManager securityManager,
            SubscriptionManager subscriptionManager,
            ISecurityService securityService,
            Resolution defaultResolution)
        {
            cashBook.Updated += (sender, updateType) =>
            {
                if (updateType == CashBook.UpdateType.Added)
                {
                    _ensureCurrencyDataFeeds = true;
                }
            };

            _defaultResolution = defaultResolution;
            _pendingSubscriptionDataConfigs = false;
            _securityManager = securityManager;
            _subscriptionManager = subscriptionManager;
            _securityService = securityService;
            _cashBook = cashBook;
            _addedCurrencySubscriptionDataConfigs = new HashSet<SubscriptionDataConfig>();
            _toBeAddedCurrencySubscriptionDataConfigs = new HashSet<SubscriptionDataConfig>();
        }

        /// <summary>
        /// Will verify if there are any <see cref="SubscriptionDataConfig"/> to be removed
        /// for a given added <see cref="Symbol"/>.
        /// </summary>
        /// <param name="addedSymbol">The symbol that was added to the data feed system</param>
        /// <returns>The SubscriptionDataConfig to be removed, null if none</returns>
        public SubscriptionDataConfig GetSubscriptionDataConfigToRemove(Symbol addedSymbol)
        {
            if (addedSymbol.SecurityType == SecurityType.Crypto
                || addedSymbol.SecurityType == SecurityType.Forex
                || addedSymbol.SecurityType == SecurityType.Cfd)
            {
                var currencyDataFeed = _addedCurrencySubscriptionDataConfigs
                    .FirstOrDefault(x => x.Symbol == addedSymbol);
                if (currencyDataFeed != null)
                {
                    return currencyDataFeed;
                }
            }
            return null;
        }

        /// <summary>
        /// Will update pending currency <see cref="SubscriptionDataConfig"/>
        /// </summary>
        /// <returns>True when there are pending currency subscriptions <see cref="GetPendingSubscriptionDataConfigs"/></returns>
        public bool UpdatePendingSubscriptionDataConfigs(IBrokerageModel brokerageModel)
        {
            if (_ensureCurrencyDataFeeds)
            {
                // this allows us to handle the case where SetCash is called when no security has been really added
                EnsureCurrencySubscriptionDataConfigs(SecurityChanges.None, brokerageModel);
            }
            return _pendingSubscriptionDataConfigs;
        }

        /// <summary>
        /// Will return any pending internal currency <see cref="SubscriptionDataConfig"/> and remove them as pending.
        /// </summary>
        /// <returns>Will return the <see cref="SubscriptionDataConfig"/> to be added</returns>
        public IEnumerable<SubscriptionDataConfig> GetPendingSubscriptionDataConfigs()
        {
            var result = new List<SubscriptionDataConfig>();
            if (_pendingSubscriptionDataConfigs)
            {
                foreach (var subscriptionDataConfig in _toBeAddedCurrencySubscriptionDataConfigs)
                {
                    _addedCurrencySubscriptionDataConfigs.Add(subscriptionDataConfig);
                    result.Add(subscriptionDataConfig);
                }
                _toBeAddedCurrencySubscriptionDataConfigs.Clear();
                _pendingSubscriptionDataConfigs = false;
            }
            return result;
        }

        /// <summary>
        /// Checks the current <see cref="SubscriptionDataConfig"/> and adds new necessary currency pair feeds to provide real time conversion data
        /// </summary>
        public void EnsureCurrencySubscriptionDataConfigs(SecurityChanges securityChanges, IBrokerageModel brokerageModel)
        {
            _ensureCurrencyDataFeeds = false;
            // remove any 'to be added' if the security has already been added
            _toBeAddedCurrencySubscriptionDataConfigs.RemoveWhere(
                config => securityChanges.AddedSecurities.Any(x => x.Symbol == config.Symbol));

            var newConfigs = _cashBook.EnsureCurrencyDataFeeds(
                _securityManager,
                _subscriptionManager,
                brokerageModel.DefaultMarkets,
                securityChanges,
                _securityService,
                _defaultResolution);
            foreach (var config in newConfigs)
            {
                _toBeAddedCurrencySubscriptionDataConfigs.Add(config);
            }
            _pendingSubscriptionDataConfigs = _toBeAddedCurrencySubscriptionDataConfigs.Any();
        }
    }
}
