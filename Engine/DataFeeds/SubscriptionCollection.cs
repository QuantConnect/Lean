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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides a collection for holding subscriptions.
    /// </summary>
    public class SubscriptionCollection : IEnumerable<Subscription>
    {
        private readonly ConcurrentDictionary<Symbol, ConcurrentDictionary<SubscriptionDataConfig, Subscription>> _subscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionCollection"/> class
        /// </summary>
        public SubscriptionCollection()
        {
            _subscriptions = new ConcurrentDictionary<Symbol, ConcurrentDictionary<SubscriptionDataConfig, Subscription>>();
        }

        /// <summary>
        /// Checks the collection for the specified subscription configuration
        /// </summary>
        /// <param name="configuration">The subscription configuration to check for</param>
        /// <returns>True if a subscription with the specified configuration is found in this collection, false otherwise</returns>
        public bool Contains(SubscriptionDataConfig configuration)
        {
            ConcurrentDictionary<SubscriptionDataConfig, Subscription> dictionary;
            if (!_subscriptions.TryGetValue(configuration.Symbol, out dictionary))
            {
                return false;
            }

            return dictionary.ContainsKey(configuration);
        }

        /// <summary>
        /// Checks the collection for any subscriptions with the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to check</param>
        /// <returns>True if any subscriptions are found with the specified symbol</returns>
        public bool ContainsAny(Symbol symbol)
        {
            return _subscriptions.ContainsKey(symbol);
        }

        /// <summary>
        /// Attempts to add the specified subscription to the collection. If another subscription
        /// exists with the same configuration then it won't be added.
        /// </summary>
        /// <param name="subscription">The subscription to add</param>
        /// <returns>True if the subscription is successfully added, false otherwise</returns>
        public bool TryAdd(Subscription subscription)
        {
            ConcurrentDictionary<SubscriptionDataConfig, Subscription> dictionary;
            if (!_subscriptions.TryGetValue(subscription.Configuration.Symbol, out dictionary))
            {
                dictionary = new ConcurrentDictionary<SubscriptionDataConfig, Subscription>();
                _subscriptions[subscription.Configuration.Symbol] = dictionary;
            }

            return dictionary.TryAdd(subscription.Configuration, subscription);
        }

        /// <summary>
        /// Attempts to retrieve the subscription with the specified configuration
        /// </summary>
        /// <param name="configuration">The subscription's configuration</param>
        /// <param name="subscription">The subscription matching the configuration, null if not found</param>
        /// <returns>True if the subscription is successfully retrieved, false otherwise</returns>
        public bool TryGetValue(SubscriptionDataConfig configuration, out Subscription subscription)
        {
            ConcurrentDictionary<SubscriptionDataConfig, Subscription> dictionary;
            if (!_subscriptions.TryGetValue(configuration.Symbol, out dictionary))
            {
                subscription = null;
                return false;
            }

            return dictionary.TryGetValue(configuration, out subscription);
        }

        /// <summary>
        /// Attempts to retrieve the subscription with the specified configuration
        /// </summary>
        /// <param name="symbol">The symbol of the subscription's configuration</param>
        /// <param name="subscriptions">The subscriptions matching the symbol, null if not found</param>
        /// <returns>True if the subscriptions are successfully retrieved, false otherwise</returns>
        public bool TryGetAll(Symbol symbol, out ICollection<Subscription> subscriptions)
        {
            ConcurrentDictionary<SubscriptionDataConfig, Subscription> dictionary;
            if (!_subscriptions.TryGetValue(symbol, out dictionary))
            {
                subscriptions = null;
                return false;
            }

            subscriptions = dictionary.Select(x => x.Value).ToList();
            return true;
        }

        /// <summary>
        /// Attempts to remove the subscription with the specified configuraton from the collection.
        /// </summary>
        /// <param name="configuration">The configuration of the subscription to remove</param>
        /// <param name="subscription">The removed subscription, null if not found.</param>
        /// <returns>True if the subscription is successfully removed, false otherwise</returns>
        public bool TryRemove(SubscriptionDataConfig configuration, out Subscription subscription)
        {
            ConcurrentDictionary<SubscriptionDataConfig, Subscription> dictionary;
            if (!_subscriptions.TryGetValue(configuration.Symbol, out dictionary))
            {
                subscription = null;
                return false;
            }

            if (!dictionary.TryRemove(configuration, out subscription))
            {
                subscription = null;
                return false;
            }

            return dictionary.Any() || _subscriptions.TryRemove(configuration.Symbol, out dictionary);
        }

        /// <summary>
        /// Attempts to remove all subscriptons for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol of the subscriptions to remove</param>
        /// <param name="subscriptions">The removed subscriptions</param>
        /// <returns></returns>
        public bool TryRemoveAll(Symbol symbol, out ICollection<Subscription> subscriptions)
        {
            ConcurrentDictionary<SubscriptionDataConfig, Subscription> dictionary;
            if (!_subscriptions.TryRemove(symbol, out dictionary))
            {
                subscriptions = null;
                return false;
            }

            subscriptions = dictionary.Select(x => x.Value).ToList();
            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Subscription> GetEnumerator()
        {
            foreach (var subscriptionsByConfig in _subscriptions
                .Select(x => x.Value))
            {
                foreach (var subscription in subscriptionsByConfig
                    .Select(x => x.Value)
                    .OrderBy(x => x.Configuration.TickType))
                {
                    yield return subscription;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
