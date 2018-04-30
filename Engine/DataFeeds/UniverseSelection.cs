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
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Util;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides methods for apply the results of universe selection to an algorithm
    /// </summary>
    public class UniverseSelection
    {
        private readonly IDataFeed _dataFeed;
        private readonly IAlgorithm _algorithm;
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
        private readonly HashSet<Security> _pendingRemovals = new HashSet<Security>();
        private readonly Dictionary<DateTime, Dictionary<Symbol, Security>> _pendingSecurityAdditions = new Dictionary<DateTime, Dictionary<Symbol, Security>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSelection"/> class
        /// </summary>
        /// <param name="dataFeed">The data feed to add/remove subscriptions from</param>
        /// <param name="algorithm">The algorithm to add securities to</param>
        public UniverseSelection(IDataFeed dataFeed, IAlgorithm algorithm)
        {
            _dataFeed = dataFeed;
            _algorithm = algorithm;
        }

        /// <summary>
        /// Applies universe selection the the data feed and algorithm
        /// </summary>
        /// <param name="universe">The universe to perform selection on</param>
        /// <param name="dateTimeUtc">The current date time in utc</param>
        /// <param name="universeData">The data provided to perform selection with</param>
        public SecurityChanges ApplyUniverseSelection(Universe universe, DateTime dateTimeUtc, BaseDataCollection universeData)
        {
            var algorithmEndDateUtc = _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone);
            if (dateTimeUtc > algorithmEndDateUtc)
            {
                return SecurityChanges.None;
            }

            IEnumerable<Symbol> selectSymbolsResult;

            // check if this universe must be filtered with fine fundamental data
            var fineFiltered = universe as FineFundamentalFilteredUniverse;
            if (fineFiltered != null)
            {
                // perform initial filtering and limit the result
                selectSymbolsResult = universe.SelectSymbols(dateTimeUtc, universeData);

                if (!ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
                {
                    // prepare a BaseDataCollection of FineFundamental instances
                    var fineCollection = new BaseDataCollection();
                    var dataProvider = new DefaultDataProvider();

                    foreach (var symbol in selectSymbolsResult)
                    {
                        var config = FineFundamentalUniverse.CreateConfiguration(symbol);

                        var exchangeHours = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.ID.SecurityType).ExchangeHours;
                        var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.ID.SecurityType, CashBook.AccountCurrency);
                        var quoteCash = _algorithm.Portfolio.CashBook[symbolProperties.QuoteCurrency];

                        var security = new Equity(symbol, exchangeHours, quoteCash, symbolProperties);

                        var localStartTime = dateTimeUtc.ConvertFromUtc(exchangeHours.TimeZone).AddDays(-1);
                        var factory = new FineFundamentalSubscriptionEnumeratorFactory(_algorithm.LiveMode, x => new[] { localStartTime });
                        var request = new SubscriptionRequest(true, universe, security, new SubscriptionDataConfig(config), localStartTime, localStartTime);
                        using (var enumerator = factory.CreateEnumerator(request, dataProvider))
                        {
                            if (enumerator.MoveNext())
                            {
                                fineCollection.Data.Add(enumerator.Current);
                            }
                        }
                    }

                    // WARNING -- HACK ATTACK -- WARNING
                    // Fine universes are considered special due to their chaining behavior.
                    // As such, we need a means of piping the fine data read in here back to the data feed
                    // so that it can be properly emitted via a TimeSlice.Create call. There isn't a mechanism
                    // in place for this function to return such data. The following lines are tightly coupled
                    // to the universeData dictionaries in SubscriptionSynchronizer and LiveTradingDataFeed and
                    // rely on reference semantics to work.

                    // Coarse raw data has SID collision on: CRHCY R735QTJ8XC9X
                    var coarseData = universeData.Data.OfType<CoarseFundamental>()
                        .DistinctBy(c => c.Symbol)
                        .ToDictionary(c => c.Symbol);

                    universeData.Data = new List<BaseData>();
                    foreach (var fine in fineCollection.Data.OfType<FineFundamental>())
                    {
                        var fundamentals = new Fundamentals
                        {
                            Symbol = fine.Symbol,
                            Time = fine.Time,
                            EndTime = fine.EndTime,
                            DataType = fine.DataType,
                            CompanyReference = fine.CompanyReference,
                            EarningReports = fine.EarningReports,
                            EarningRatios = fine.EarningRatios,
                            FinancialStatements = fine.FinancialStatements,
                            OperationRatios = fine.OperationRatios,
                            SecurityReference = fine.SecurityReference,
                            ValuationRatios = fine.ValuationRatios
                        };

                        CoarseFundamental coarse;
                        if (coarseData.TryGetValue(fine.Symbol, out coarse))
                        {
                            // the only time the coarse data won't exist is if the selection function
                            // doesn't use the data provided, and instead returns a constant list of
                            // symbols -- coupled with a potential hole in the data
                            fundamentals.Value = coarse.Value;
                            fundamentals.Market = coarse.Market;
                            fundamentals.Volume = coarse.Volume;
                            fundamentals.DollarVolume = coarse.DollarVolume;
                            fundamentals.HasFundamentalData = coarse.HasFundamentalData;

                            // set the fine fundamental price property to yesterday's closing price
                            fine.Value = coarse.Value;
                        }

                        universeData.Data.Add(fundamentals);
                    }

                    // END -- HACK ATTACK -- END

                    // perform the fine fundamental universe selection
                    selectSymbolsResult = fineFiltered.FineFundamentalUniverse.PerformSelection(dateTimeUtc, fineCollection);
                }
            }
            else
            {
                // perform initial filtering and limit the result
                selectSymbolsResult = universe.PerformSelection(dateTimeUtc, universeData);
            }

            // check for no changes first
            if (ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
            {
                return SecurityChanges.None;
            }

            // materialize the enumerable into a set for processing
            var selections = selectSymbolsResult.ToHashSet();

            var additions = new List<Security>();
            var removals = new List<Security>();

            // remove previously deselected members which were kept in the universe because of holdings or open orders
            foreach (var member in _pendingRemovals.ToList())
            {
                if (IsSafeToRemove(member))
                {
                    RemoveSecurityFromUniverse(universe, member, removals, dateTimeUtc, algorithmEndDateUtc);

                    _pendingRemovals.Remove(member);
                }
            }

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

                if (IsSafeToRemove(member))
                {
                    RemoveSecurityFromUniverse(universe, member, removals, dateTimeUtc, algorithmEndDateUtc);
                }
                else
                {
                    _pendingRemovals.Add(member);
                }
            }

            var keys = _pendingSecurityAdditions.Keys;
            if (keys.Any() && keys.Single() != dateTimeUtc)
            {
                // if the frontier moved forward then we've added these securities to the algorithm
                _pendingSecurityAdditions.Clear();
            }

            Dictionary<Symbol, Security> pendingAdditions;
            if (!_pendingSecurityAdditions.TryGetValue(dateTimeUtc, out pendingAdditions))
            {
                // keep track of created securities so we don't create the same security twice, leads to bad things :)
                pendingAdditions = new Dictionary<Symbol, Security>();
                _pendingSecurityAdditions[dateTimeUtc] = pendingAdditions;
            }

            // find new selections and add them to the algorithm
            foreach (var symbol in selections)
            {
                // create the new security, the algorithm thread will add this at the appropriate time
                Security security;
                if (!pendingAdditions.TryGetValue(symbol, out security) && !_algorithm.Securities.TryGetValue(symbol, out security))
                {
                    security = universe.CreateSecurity(symbol, _algorithm, _marketHoursDatabase, _symbolPropertiesDatabase);
                    pendingAdditions.Add(symbol, security);
                }

                var addedSubscription = false;

                foreach (var request in universe.GetSubscriptionRequests(security, dateTimeUtc, algorithmEndDateUtc))
                {
                    // add the new subscriptions to the data feed
                    _dataFeed.AddSubscription(request);

                    // only update our security changes if we actually added data
                    if (!request.IsUniverseSubscription)
                    {
                        addedSubscription = true;
                    }
                }

                if (addedSubscription)
                {
                    var addedMember = universe.AddMember(dateTimeUtc, security);

                    if (addedMember)
                    {
                        additions.Add(security);
                    }
                }
            }

            // return None if there's no changes, otherwise return what we've modified
            var securityChanges = additions.Count + removals.Count != 0
                ? new SecurityChanges(additions, removals)
                : SecurityChanges.None;

            // Add currency data feeds that weren't explicitly added in Initialize
            if (additions.Count > 0)
            {
                var addedSecurities = _algorithm.Portfolio.CashBook.EnsureCurrencyDataFeeds(_algorithm.Securities, _algorithm.SubscriptionManager, _marketHoursDatabase, _symbolPropertiesDatabase, _algorithm.BrokerageModel.DefaultMarkets, securityChanges);
                foreach (var security in addedSecurities)
                {
                    // assume currency feeds are always one subscription per, these are typically quote subscriptions
                    _dataFeed.AddSubscription(new SubscriptionRequest(false, universe, security, new SubscriptionDataConfig(security.Subscriptions.First()), dateTimeUtc, algorithmEndDateUtc));
                }
            }

            if (securityChanges != SecurityChanges.None)
            {
                Log.Debug("UniverseSelection.ApplyUniverseSelection(): " + dateTimeUtc + ": " + securityChanges);
            }

            return securityChanges;
        }

        private void RemoveSecurityFromUniverse(Universe universe, Security member, List<Security> removals, DateTime dateTimeUtc, DateTime algorithmEndDateUtc)
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

        /// <summary>
        /// Determines if we can safely remove the security member from a universe.
        /// We must ensure that we have zero holdings, no open orders, and no existing portfolio targets
        /// </summary>
        private bool IsSafeToRemove(Security member)
        {
            // but don't physically remove it from the algorithm if we hold stock or have open orders against it or an open target
            var openOrders = _algorithm.Transactions.GetOrders(x => x.Status.IsOpen() && x.Symbol == member.Symbol);
            if (!member.HoldStock && !openOrders.Any() && (member.Holdings.Target == null || member.Holdings.Target.Quantity == 0))
            {
                return true;
            }

            return false;
        }
    }
}
