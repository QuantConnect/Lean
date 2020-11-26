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
using System.Threading.Tasks;
using QuantConnect.Benchmarks;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides methods for apply the results of universe selection to an algorithm
    /// </summary>
    public class UniverseSelection
    {
        private IDataFeedSubscriptionManager _dataManager;
        private readonly IAlgorithm _algorithm;
        private readonly ISecurityService _securityService;
        private readonly IDataPermissionManager _dataPermissionManager;
        private readonly Dictionary<DateTime, Dictionary<Symbol, Security>> _pendingSecurityAdditions = new Dictionary<DateTime, Dictionary<Symbol, Security>>();
        private readonly PendingRemovalsManager _pendingRemovalsManager;
        private readonly CurrencySubscriptionDataConfigManager _currencySubscriptionDataConfigManager;
        private readonly InternalSubscriptionManager _internalSubscriptionManager;
        private bool _initializedSecurityBenchmark;
        private readonly IDataProvider _dataProvider;
        private bool _anyDoesNotHaveFundamentalDataWarningLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseSelection"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm to add securities to</param>
        /// <param name="securityService">The security service</param>
        /// <param name="dataPermissionManager">The data permissions manager</param>
        /// <param name="dataProvider">The data provider to use</param>
        /// <param name="internalConfigResolution">The resolution to use for internal configuration</param>
        public UniverseSelection(
            IAlgorithm algorithm,
            ISecurityService securityService,
            IDataPermissionManager dataPermissionManager,
            IDataProvider dataProvider,
            Resolution internalConfigResolution = Resolution.Minute)
        {
            _dataProvider = dataProvider;
            _algorithm = algorithm;
            _securityService = securityService;
            _dataPermissionManager = dataPermissionManager;
            _pendingRemovalsManager = new PendingRemovalsManager(algorithm.Transactions);
            _currencySubscriptionDataConfigManager = new CurrencySubscriptionDataConfigManager(algorithm.Portfolio.CashBook,
                algorithm.Securities,
                algorithm.SubscriptionManager,
                _securityService,
                dataPermissionManager.GetResolution(Resolution.Minute));
            // TODO: next step is to merge currency internal subscriptions under the same 'internal manager' instance and we could move this directly into the DataManager class
            _internalSubscriptionManager = new InternalSubscriptionManager(_algorithm, internalConfigResolution);
        }

        /// <summary>
        /// Sets the data manager
        /// </summary>
        public void SetDataManager(IDataFeedSubscriptionManager dataManager)
        {
            if (_dataManager != null)
            {
                throw new Exception("UniverseSelection.SetDataManager(): can only be set once");
            }
            _dataManager = dataManager;

            _internalSubscriptionManager.Added += (sender, request) =>
            {
                _dataManager.AddSubscription(request);
            };
            _internalSubscriptionManager.Removed += (sender, request) =>
            {
                _dataManager.RemoveSubscription(request.Configuration);
            };
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

                    // Create a dictionary of CoarseFundamental keyed by Symbol that also has FineFundamental
                    // Coarse raw data has SID collision on: CRHCY R735QTJ8XC9X
                    var allCoarse = universeData.Data.OfType<CoarseFundamental>();
                    var coarseData = allCoarse.Where(c => c.HasFundamentalData)
                        .DistinctBy(c => c.Symbol)
                        .ToDictionary(c => c.Symbol);

                    // Remove selected symbols that does not have fine fundamental data
                    var anyDoesNotHaveFundamentalData = false;
                    // only pre filter selected symbols if there actually is any coarse data. This way we can support custom universe filtered by fine fundamental data
                    // which do not use coarse data as underlying, in which case it could happen that we try to load fine fundamental data that is missing, but no problem,
                    // 'FineFundamentalSubscriptionEnumeratorFactory' won't emit it
                    if (allCoarse.Any())
                    {
                        selectSymbolsResult = selectSymbolsResult
                            .Where(
                                symbol =>
                                {
                                    var result = coarseData.ContainsKey(symbol);
                                    anyDoesNotHaveFundamentalData |= !result;
                                    return result;
                                }
                            );
                    }

                    if (!_anyDoesNotHaveFundamentalDataWarningLogged && anyDoesNotHaveFundamentalData)
                    {
                        _algorithm.Debug("Note: Your coarse selection filter was updated to exclude symbols without fine fundamental data. Make sure your coarse filter excludes symbols where HasFundamental is false.");
                        _anyDoesNotHaveFundamentalDataWarningLogged = true;
                    }

                    // use all available threads, the entire system is waiting for this to complete
                    var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                    Parallel.ForEach(selectSymbolsResult, options, symbol =>
                    {
                        var config = FineFundamentalUniverse.CreateConfiguration(symbol);
                        var security = _securityService.CreateSecurity(symbol,
                            config,
                            addToSymbolCache: false);

                        var localStartTime = dateTimeUtc.ConvertFromUtc(config.ExchangeTimeZone).AddDays(-1);
                        var factory = new FineFundamentalSubscriptionEnumeratorFactory(_algorithm.LiveMode, x => new[] { localStartTime });
                        var request = new SubscriptionRequest(true, universe, security, new SubscriptionDataConfig(config), localStartTime, localStartTime);
                        using (var enumerator = factory.CreateEnumerator(request, _dataProvider))
                        {
                            if (enumerator.MoveNext())
                            {
                                lock (fineCollection.Data)
                                {
                                    fineCollection.Data.Add(enumerator.Current);
                                }
                            }
                        }
                    });

                    // WARNING -- HACK ATTACK -- WARNING
                    // Fine universes are considered special due to their chaining behavior.
                    // As such, we need a means of piping the fine data read in here back to the data feed
                    // so that it can be properly emitted via a TimeSlice.Create call. There isn't a mechanism
                    // in place for this function to return such data. The following lines are tightly coupled
                    // to the universeData dictionaries in SubscriptionSynchronizer and LiveTradingDataFeed and
                    // rely on reference semantics to work.

                    universeData.Data = new List<BaseData>();
                    foreach (var fine in fineCollection.Data.OfType<FineFundamental>())
                    {
                        var fundamentals = new Fundamentals
                        {
                            Symbol = fine.Symbol,
                            Time = fine.Time,
                            EndTime = fine.EndTime,
                            DataType = fine.DataType,
                            AssetClassification = fine.AssetClassification,
                            CompanyProfile = fine.CompanyProfile,
                            CompanyReference = fine.CompanyReference,
                            EarningReports = fine.EarningReports,
                            EarningRatios = fine.EarningRatios,
                            FinancialStatements = fine.FinancialStatements,
                            OperationRatios = fine.OperationRatios,
                            SecurityReference = fine.SecurityReference,
                            ValuationRatios = fine.ValuationRatios,
                            Market = fine.Symbol.ID.Market
                        };

                        CoarseFundamental coarse;
                        if (coarseData.TryGetValue(fine.Symbol, out coarse))
                        {
                            // the only time the coarse data won't exist is if the selection function
                            // doesn't use the data provided, and instead returns a constant list of
                            // symbols -- coupled with a potential hole in the data
                            fundamentals.Value = coarse.Value;
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

            // materialize the enumerable into a set for processing
            var selections = selectSymbolsResult.ToHashSet();

            var additions = new List<Security>();
            var removals = new List<Security>();

            // first check for no pending removals, even if the universe selection
            // didn't change we might need to remove a security because a position was closed
            RemoveSecurityFromUniverse(
                _pendingRemovalsManager.CheckPendingRemovals(selections, universe),
                removals,
                dateTimeUtc,
                algorithmEndDateUtc);

            // check for no changes second
            if (ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
            {
                return SecurityChanges.None;
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

                RemoveSecurityFromUniverse(_pendingRemovalsManager.TryRemoveMember(member, universe),
                    removals,
                    dateTimeUtc,
                    algorithmEndDateUtc);
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
                if (universe.Securities.ContainsKey(symbol))
                {
                    // if its already part of the universe no need to re add it
                    continue;
                }

                // create the new security, the algorithm thread will add this at the appropriate time
                Security security;
                if (!pendingAdditions.TryGetValue(symbol, out security) && !_algorithm.Securities.TryGetValue(symbol, out security))
                {
                    // For now this is required for retro compatibility with usages of security.Subscriptions
                    var configs = _algorithm.SubscriptionManager.SubscriptionDataConfigService.Add(symbol,
                        universe.UniverseSettings.Resolution,
                        universe.UniverseSettings.FillForward,
                        universe.UniverseSettings.ExtendedMarketHours,
                        dataNormalizationMode: universe.UniverseSettings.DataNormalizationMode);

                    security = _securityService.CreateSecurity(symbol, configs, universe.UniverseSettings.Leverage, (symbol.ID.SecurityType == SecurityType.Option || symbol.ID.SecurityType == SecurityType.FutureOption));

                    pendingAdditions.Add(symbol, security);
                }

                var addedSubscription = false;
                var dataFeedAdded = false;
                foreach (var request in universe.GetSubscriptionRequests(security, dateTimeUtc, algorithmEndDateUtc,
                                                                         _algorithm.SubscriptionManager.SubscriptionDataConfigService))
                {
                    if (security.Symbol == request.Configuration.Symbol // Just in case check its the same symbol, else AddData will throw.
                        && !security.Subscriptions.Contains(request.Configuration))
                    {
                        // For now this is required for retro compatibility with usages of security.Subscriptions
                        security.AddData(request.Configuration);
                    }

                    var toRemove = _currencySubscriptionDataConfigManager.GetSubscriptionDataConfigToRemove(request.Configuration.Symbol);
                    if (toRemove != null)
                    {
                        Log.Trace($"UniverseSelection.ApplyUniverseSelection(): Removing internal currency data feed {toRemove}");
                        _dataManager.RemoveSubscription(toRemove);
                    }

                    // 'dataFeedAdded' will help us notify the user for security changes only once per non internal subscription
                    // for example two universes adding the sample configuration, we don't want two notifications
                    dataFeedAdded = _dataManager.AddSubscription(request);

                    // only update our security changes if we actually added data
                    if (!request.IsUniverseSubscription)
                    {
                        addedSubscription = true;

                        _internalSubscriptionManager.AddedSubscriptionRequest(request);
                    }
                }

                if (addedSubscription)
                {
                    var addedMember = universe.AddMember(dateTimeUtc, security);

                    if (addedMember && dataFeedAdded)
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
                EnsureCurrencyDataFeeds(securityChanges);
            }

            if (securityChanges != SecurityChanges.None && Log.DebuggingEnabled)
            {
                // for performance lets not create the message string if debugging is not enabled
                // this can be executed many times and its in the algorithm thread
                Log.Debug("UniverseSelection.ApplyUniverseSelection(): " + dateTimeUtc + ": " + securityChanges);
            }

            return securityChanges;
        }

        /// <summary>
        /// Will add any pending internal currency subscriptions
        /// </summary>
        /// <param name="utcStart">The current date time in utc</param>
        /// <returns>Will return true if any subscription was added</returns>
        public bool AddPendingInternalDataFeeds(DateTime utcStart)
        {
            var added = false;
            if (!_initializedSecurityBenchmark)
            {
                _initializedSecurityBenchmark = true;

                var securityBenchmark = _algorithm.Benchmark as SecurityBenchmark;
                if (securityBenchmark != null)
                {
                    var dataConfig = _algorithm.SubscriptionManager.SubscriptionDataConfigService.Add(
                        securityBenchmark.Security.Symbol,
                        _dataPermissionManager.GetResolution(_algorithm.LiveMode ? Resolution.Minute : Resolution.Hour),
                        isInternalFeed: true,
                        fillForward: false).First();

                    // we want to start from the previous tradable bar so the benchmark security
                    // never has 0 price
                    var previousTradableBar = Time.GetStartTimeForTradeBars(
                        securityBenchmark.Security.Exchange.Hours,
                        utcStart.ConvertFromUtc(securityBenchmark.Security.Exchange.TimeZone),
                        _algorithm.LiveMode ? Time.OneMinute : Time.OneDay,
                        1,
                        false,
                        dataConfig.DataTimeZone).ConvertToUtc(securityBenchmark.Security.Exchange.TimeZone);

                    if (dataConfig != null)
                    {
                        added |= _dataManager.AddSubscription(new SubscriptionRequest(
                            false,
                            null,
                            securityBenchmark.Security,
                            dataConfig,
                            previousTradableBar,
                            _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone)));

                        Log.Trace($"UniverseSelection.AddPendingInternalDataFeeds(): Adding internal benchmark data feed {dataConfig}");
                    }
                }
            }

            if (_currencySubscriptionDataConfigManager.UpdatePendingSubscriptionDataConfigs(_algorithm.BrokerageModel))
            {
                foreach (var subscriptionDataConfig in _currencySubscriptionDataConfigManager
                    .GetPendingSubscriptionDataConfigs())
                {
                    var security = _algorithm.Securities[subscriptionDataConfig.Symbol];
                    added |= _dataManager.AddSubscription(new SubscriptionRequest(
                        false,
                        null,
                        security,
                        subscriptionDataConfig,
                        utcStart,
                        _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone)));
                }
            }
            return added;
        }

        /// <summary>
        /// Checks the current subscriptions and adds necessary currency pair feeds to provide real time conversion data
        /// </summary>
        public void EnsureCurrencyDataFeeds(SecurityChanges securityChanges)
        {
            _currencySubscriptionDataConfigManager.EnsureCurrencySubscriptionDataConfigs(securityChanges, _algorithm.BrokerageModel);
        }

        private void RemoveSecurityFromUniverse(
            List<PendingRemovalsManager.RemovedMember> removedMembers,
            List<Security> removals,
            DateTime dateTimeUtc,
            DateTime algorithmEndDateUtc)
        {
            foreach (var removedMember in removedMembers)
            {
                var universe = removedMember.Universe;
                var member = removedMember.Security;

                // safe to remove the member from the universe
                universe.RemoveMember(dateTimeUtc, member);

                // we need to mark this security as untradeable while it has no data subscription
                // it is expected that this function is called while in sync with the algo thread,
                // so we can make direct edits to the security here
                member.Cache.Reset();
                foreach (var subscription in universe.GetSubscriptionRequests(member, dateTimeUtc, algorithmEndDateUtc,
                                                                              _algorithm.SubscriptionManager.SubscriptionDataConfigService))
                {
                    if (subscription.IsUniverseSubscription)
                    {
                        removals.Remove(member);
                    }
                    else
                    {
                        if (_dataManager.RemoveSubscription(subscription.Configuration, universe))
                        {
                            _internalSubscriptionManager.RemovedSubscriptionRequest(subscription);
                            member.IsTradable = false;
                        }
                    }
                }

                // remove symbol mappings for symbols removed from universes // TODO : THIS IS BAD!
                SymbolCache.TryRemove(member.Symbol);
            }
        }
    }
}
