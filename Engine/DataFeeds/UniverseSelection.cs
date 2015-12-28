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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides methods for apply the results of universe selection to an algorithm
    /// </summary>
    public class UniverseSelection
    {
        private readonly IDataFeed _dataFeed;
        private readonly IAlgorithm _algorithm;
        private readonly SubscriptionLimiter _limiter;
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSelection"/> class
        /// </summary>
        /// <param name="dataFeed">The data feed to add/remove subscriptions from</param>
        /// <param name="algorithm">The algorithm to add securities to</param>
        /// <param name="controls">Specifies limits on the algorithm's memory usage</param>
        public UniverseSelection(IDataFeed dataFeed, IAlgorithm algorithm, Controls controls)
        {
            _dataFeed = dataFeed;
            _algorithm = algorithm;
            _limiter = new SubscriptionLimiter(() => dataFeed.Subscriptions, controls.TickLimit, controls.SecondLimit, controls.MinuteLimit);
        }

        /// <summary>
        /// Applies universe selection the the data feed and algorithm
        /// </summary>
        /// <param name="universe">The universe to perform selection on</param>
        /// <param name="dateTimeUtc">The current date time in utc</param>
        /// <param name="universeData">The data provided to perform selection with</param>
        public SecurityChanges ApplyUniverseSelection(Universe universe, DateTime dateTimeUtc, BaseDataCollection universeData)
        {
            var settings = universe.UniverseSettings;

            // perform initial filtering and limit the result
            var selectSymbolsResult = universe.SelectSymbols(dateTimeUtc, universeData.Data);

            // check for no changes first
            if (ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
            {
                return SecurityChanges.None;
            }

            // materialize the enumerable into a set for processing
            var selections = selectSymbolsResult.ToHashSet();

            // create a hash set of our existing subscriptions by sid
            var existingSubscriptions = _dataFeed.Subscriptions.ToHashSet(x => x.Security.Symbol);

            var additions = new List<Security>();
            var removals = new List<Security>();

            // determine which data subscriptions need to be removed for this market
            foreach (var subscription in _dataFeed.Subscriptions)
            {
                // universes can only remove members of their own
                if (!universe.ContainsMember(subscription.Security)) continue;

                // never remove universe selection subscriptions
                if (subscription.IsUniverseSelectionSubscription) continue;

                var config = subscription.Configuration;

                // never remove internal feeds
                if (config.IsInternalFeed) continue;

                // if we've selected this subscription again, keep it
                if (selections.Contains(config.Symbol)) continue;

                // don't remove if the universe wants to keep him in
                if (!universe.CanRemoveMember(dateTimeUtc, subscription.Security)) continue;

                // let the algorithm know this security has been removed from the universe
                removals.Add(subscription.Security);

                // but don't physically remove it from the algorithm if we hold stock or have open orders against it
                var openOrders = _algorithm.Transactions.GetOrders(x => x.Status.IsOpen() && x.Symbol == config.Symbol);
                if (!subscription.Security.HoldStock && !openOrders.Any())
                {
                    // we need to mark this security as untradeable while it has no data subscription
                    // it is expected that this function is called while in sync with the algo thread,
                    // so we can make direct edits to the security here
                    subscription.Security.Cache.Reset();

                    if (_dataFeed.RemoveSubscription(subscription))
                    {
                        universe.RemoveMember(dateTimeUtc, subscription.Security);
                    }
                }
            }

            // find new selections and add them to the algorithm
            foreach (var symbol in selections)
            {
                // we already have a subscription for this symbol so don't re-add it
                if (existingSubscriptions.Contains(symbol)) continue;

                // ask the limiter if we can add another subscription at that resolution
                string reason;
                if (!_limiter.CanAddSubscription(settings.Resolution, out reason))
                {
                    _algorithm.Error(reason);
                    Log.Trace("UniverseSelection.ApplyUniverseSelection(): Skipping adding subscriptions: " + reason);
                    break;
                }
                
                // create the new security, the algorithm thread will add this at the appropriate time
                Security security;
                if (!_algorithm.Securities.TryGetValue(symbol, out security))
                {
                    security = SecurityManager.CreateSecurity(_algorithm.Portfolio, _algorithm.SubscriptionManager, _marketHoursDatabase,
                        symbol,
                        settings.Resolution,
                        settings.FillForward,
                        settings.Leverage,
                        settings.ExtendedMarketHours,
                        false,
                        false,
                        false);

                    // added securities should use the brokerage model provided models
                    _algorithm.UpdateModel(_algorithm.BrokerageModel, security);
                }

                additions.Add(security);

                // add the new subscriptions to the data feed
                if (_dataFeed.AddSubscription(universe, security, dateTimeUtc, _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone)))
                {
                    universe.AddMember(dateTimeUtc, security);
                }
            }

            // Add currency data feeds that weren't explicitly added in Initialize
            if (additions.Count > 0)
            {
                var addedSecurities = _algorithm.Portfolio.CashBook.EnsureCurrencyDataFeeds(_algorithm.Securities, _algorithm.SubscriptionManager, MarketHoursDatabase.FromDataFolder());
                foreach (var security in addedSecurities)
                {
                    _dataFeed.AddSubscription(universe, security, dateTimeUtc, _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone));
                }
            }

            // return None if there's no changes, otherwise return what we've modified
            return additions.Count + removals.Count != 0
                ? new SecurityChanges(additions, removals)
                : SecurityChanges.None;
        }
    }
}
