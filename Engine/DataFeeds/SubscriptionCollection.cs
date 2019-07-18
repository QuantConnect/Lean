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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides a collection for holding subscriptions.
    /// </summary>
    public class SubscriptionCollection : IEnumerable<Subscription>
    {
        private readonly ConcurrentDictionary<SubscriptionDataConfig, Subscription> _subscriptions;
        private bool _sortingSubscriptionRequired;
        private readonly Ref<TimeSpan> _fillForwardResolution;

        // some asset types (options, futures, crypto) have multiple subscriptions for different tick types,
        // we keep a sorted list of subscriptions so we can return them in a deterministic order
        private List<Subscription> _subscriptionsByTickType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionCollection"/> class
        /// </summary>
        public SubscriptionCollection()
        {
            _subscriptions = new ConcurrentDictionary<SubscriptionDataConfig, Subscription>();
            _subscriptionsByTickType = new List<Subscription>();
            var ffres = Time.OneMinute;
            _fillForwardResolution = Ref.Create(() => ffres, res => ffres = res);
        }

        /// <summary>
        /// Checks the collection for the specified subscription configuration
        /// </summary>
        /// <param name="configuration">The subscription configuration to check for</param>
        /// <returns>True if a subscription with the specified configuration is found in this collection, false otherwise</returns>
        public bool Contains(SubscriptionDataConfig configuration)
        {
            return _subscriptions.ContainsKey(configuration);
        }

        /// <summary>
        /// Attempts to add the specified subscription to the collection. If another subscription
        /// exists with the same configuration then it won't be added.
        /// </summary>
        /// <param name="subscription">The subscription to add</param>
        /// <returns>True if the subscription is successfully added, false otherwise</returns>
        public bool TryAdd(Subscription subscription)
        {
            if (_subscriptions.TryAdd(subscription.Configuration, subscription))
            {
                UpdateFillForwardResolution(FillForwardResolutionOperation.AfterAdd, subscription.Configuration);
                _sortingSubscriptionRequired = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the subscription with the specified configuration
        /// </summary>
        /// <param name="configuration">The subscription's configuration</param>
        /// <param name="subscription">The subscription matching the configuration, null if not found</param>
        /// <returns>True if the subscription is successfully retrieved, false otherwise</returns>
        public bool TryGetValue(SubscriptionDataConfig configuration, out Subscription subscription)
        {
            return _subscriptions.TryGetValue(configuration, out subscription);
        }

        /// <summary>
        /// Attempts to remove the subscription with the specified configuraton from the collection.
        /// </summary>
        /// <param name="configuration">The configuration of the subscription to remove</param>
        /// <param name="subscription">The removed subscription, null if not found.</param>
        /// <returns>True if the subscription is successfully removed, false otherwise</returns>
        public bool TryRemove(SubscriptionDataConfig configuration, out Subscription subscription)
        {
            if (_subscriptions.TryRemove(configuration, out subscription))
            {
                UpdateFillForwardResolution(FillForwardResolutionOperation.AfterRemove, configuration);
                _sortingSubscriptionRequired = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Subscription> GetEnumerator()
        {
            SortSubscriptions();
            return _subscriptionsByTickType.GetEnumerator();
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

        /// <summary>
        /// Gets and updates the fill forward resolution by checking specified subscription configurations and
        /// selecting the smallest resoluton not equal to tick
        /// </summary>
        public Ref<TimeSpan> UpdateAndGetFillForwardResolution(SubscriptionDataConfig configuration = null)
        {
            if (configuration != null)
            {
                UpdateFillForwardResolution(FillForwardResolutionOperation.BeforeAdd, configuration);
            }
            return _fillForwardResolution;
        }

        /// <summary>
        /// Helper method to validate a configuration to be included in the fill forward calculation
        /// </summary>
        private static bool ValidateFillForwardResolution(SubscriptionDataConfig configuration)
        {
            return !configuration.IsInternalFeed && configuration.Resolution != Resolution.Tick;
        }
        /// <summary>
        /// Gets and updates the fill forward resolution by checking specified subscription configurations and
        /// selecting the smallest resoluton not equal to tick
        /// </summary>
        private void UpdateFillForwardResolution(FillForwardResolutionOperation operation, SubscriptionDataConfig configuration)
        {
            // Due to performance implications let's be jealous in updating the _fillForwardResolution
            if (ValidateFillForwardResolution(configuration) &&
                (
                    (new[] { FillForwardResolutionOperation.BeforeAdd, FillForwardResolutionOperation.AfterAdd }.Contains(operation)
                     && configuration.Increment != _fillForwardResolution.Value) // check if the new Increment is different
                ||
                    (operation == FillForwardResolutionOperation.AfterRemove // We are removing
                    && configuration.Increment == _fillForwardResolution.Value // True: We are removing the resolution we were using
                    && _subscriptions.All(x => x.Key.Resolution != configuration.Resolution))) // False: there is at least another one equal, no need to update
                )
            {
                var configurations = (operation == FillForwardResolutionOperation.BeforeAdd)
                    ? _subscriptions.Keys.Concat(new[] { configuration }) : _subscriptions.Keys;

                _fillForwardResolution.Value = configurations.Where(ValidateFillForwardResolution)
                                                             .Select(x => x.Resolution)
                                                             .Distinct()
                                                             .DefaultIfEmpty(Resolution.Minute)
                                                             .Min().ToTimeSpan();
            }
        }

        /// <summary>
        /// Sorts subscriptions so that equity subscriptions are enumerated before option
        /// securities to ensure the underlying data is available when we process the options data
        /// </summary>
        private void SortSubscriptions()
        {
            if (_sortingSubscriptionRequired)
            {
                _sortingSubscriptionRequired = false;
                // it's important that we enumerate underlying securities before derivatives to this end,
                // we order by security type so that equity subscriptions are enumerated before option
                // securities to ensure the underlying data is available when we process the options data
                _subscriptionsByTickType = _subscriptions
                    .Select(x => x.Value)
                    .OrderBy(x => x.Configuration.SecurityType)
                    .ThenBy(x => x.Configuration.TickType)
                    .ThenBy(x => x.Configuration.Symbol)
                    .ToList();
            }
        }

        private enum FillForwardResolutionOperation
        {
            AfterRemove,
            BeforeAdd,
            AfterAdd
        }
    }
}
