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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
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
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

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
            // perform initial filtering and limit the result
            var selectSymbolsResult = universe.PerformSelection(dateTimeUtc, universeData);

            // check for no changes first
            if (ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
            {
                return SecurityChanges.None;
            }

            // materialize the enumerable into a set for processing
            var selections = selectSymbolsResult.ToHashSet();

            var additions = new List<Security>();
            var removals = new List<Security>();
            var algorithmEndDateUtc = _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone);

            // determine which data subscriptions need to be removed from this universe
            foreach (var member in universe.Members.Values)
            {
                // if we've selected this subscription again, keep it
                if (selections.Contains(member.Symbol)) continue;

                // don't remove if the universe wants to keep him in
                if (!universe.CanRemoveMember(dateTimeUtc, member)) continue;

                // remove the member - this marks this member as not being
                // selected by the universe, but it may remain in the universe
                // until open orders are closed and the security is liquidated
                removals.Add(member);

                // but don't physically remove it from the algorithm if we hold stock or have open orders against it
                var openOrders = _algorithm.Transactions.GetOrders(x => x.Status.IsOpen() && x.Symbol == member.Symbol);
                if (!member.HoldStock && !openOrders.Any())
                {
                    // safe to remove the member from the universe
                    universe.RemoveMember(dateTimeUtc, member);

                    // we need to mark this security as untradeable while it has no data subscription
                    // it is expected that this function is called while in sync with the algo thread,
                    // so we can make direct edits to the security here
                    member.Cache.Reset();
                    foreach (var subscription in universe.GetSubscriptionRequests(member, dateTimeUtc, algorithmEndDateUtc))
                    {
                        if (subscription.IsUniverseSubscription)
                        {
                            removals.Remove(member);
                        }
                        else
                        {
                            _dataFeed.RemoveSubscription(subscription.Configuration);
                        }
                    }

                    // remove symbol mappings for symbols removed from universes // TODO : THIS IS BAD!
                    SymbolCache.TryRemove(member.Symbol);
                }
            }

            // find new selections and add them to the algorithm
            foreach (var symbol in selections)
            {
                // create the new security, the algorithm thread will add this at the appropriate time
                Security security;
                if (!_algorithm.Securities.TryGetValue(symbol, out security))
                {
                    security = universe.CreateSecurity(symbol, _algorithm, _marketHoursDatabase, _symbolPropertiesDatabase);
                }

                var addedMember = universe.AddMember(dateTimeUtc, security);

                var addedSubscription = false;
                foreach (var request in universe.GetSubscriptionRequests(security, dateTimeUtc, algorithmEndDateUtc))
                {
                    // ask the limiter if we can add another subscription at that resolution
                    string reason;
                    if (!_limiter.CanAddSubscription(request.Configuration.Resolution, out reason))
                    {
                        // should we be counting universe subscriptions against user subscriptions limits?

                        _algorithm.Error(reason);
                        Log.Trace("UniverseSelection.ApplyUniverseSelection(): Skipping adding subscription: " + request.Configuration.Symbol.ToString() + ": " + reason);
                        continue;
                    }

                    // add the new subscriptions to the data feed
                    _dataFeed.AddSubscription(request);

                    // only update our security changes if we actually added data
                    if (!request.IsUniverseSubscription)
                    {
                        addedSubscription = addedMember;
                    }
                }

                if (addedSubscription)
                {
                    additions.Add(security);
                }
            }

            // Add currency data feeds that weren't explicitly added in Initialize
            if (additions.Count > 0)
            {
                var addedSecurities = _algorithm.Portfolio.CashBook.EnsureCurrencyDataFeeds(_algorithm.Securities, _algorithm.SubscriptionManager, _marketHoursDatabase, _symbolPropertiesDatabase, _algorithm.BrokerageModel.DefaultMarkets);
                foreach (var security in addedSecurities)
                {
                    // assume currency feeds are always one subscription per, these are typically quote subscriptions
                    _dataFeed.AddSubscription(new SubscriptionRequest(false, universe, security, security.Subscriptions.First(), dateTimeUtc, algorithmEndDateUtc));
                }
            }

            // return None if there's no changes, otherwise return what we've modified
            var securityChanges = additions.Count + removals.Count != 0
                ? new SecurityChanges(additions, removals)
                : SecurityChanges.None;

            if (securityChanges != SecurityChanges.None)
            {
                Log.Debug("UniverseSelection.ApplyUniverseSelection(): " + dateTimeUtc + ": " + securityChanges);
            }

            return securityChanges;
        }
    }
}
