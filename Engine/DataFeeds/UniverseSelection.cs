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
using QuantConnect.Benchmarks;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
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
        private readonly Dictionary<DateTime, Dictionary<Symbol, Security>> _pendingSecurityAdditions = new Dictionary<DateTime, Dictionary<Symbol, Security>>();
        private readonly PendingRemovalsManager _pendingRemovalsManager;
        private readonly CurrencySubscriptionDataConfigManager _currencySubscriptionDataConfigManager;
        private readonly InternalSubscriptionManager _internalSubscriptionManager;
        private bool _initializedSecurityBenchmark;
        private bool _anyDoesNotHaveFundamentalDataWarningLogged;
        private readonly SecurityChangesConstructor _securityChangesConstructor;

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
            _algorithm = algorithm;
            _securityService = securityService;
            _pendingRemovalsManager = new PendingRemovalsManager(algorithm.Transactions);
            _currencySubscriptionDataConfigManager = new CurrencySubscriptionDataConfigManager(algorithm.Portfolio.CashBook,
                algorithm.Securities,
                algorithm.SubscriptionManager,
                _securityService,
                Resolution.Minute);
            // TODO: next step is to merge currency internal subscriptions under the same 'internal manager' instance and we could move this directly into the DataManager class
            _internalSubscriptionManager = new InternalSubscriptionManager(_algorithm, internalConfigResolution);
            _securityChangesConstructor = new SecurityChangesConstructor();
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
            Universe fineFiltered = (universe as FineFundamentalFilteredUniverse)?.FineFundamentalUniverse;
            fineFiltered ??= (universe as FundamentalFilteredUniverse)?.FundamentalUniverse;

            if (fineFiltered != null
                // if the universe has been disposed we don't perform selection. This us handled bellow by 'Universe.PerformSelection'
                // but in this case we directly call 'SelectSymbols' because we want to perform fine selection even if coarse returns the same
                // symbols, see 'Universe.PerformSelection', which detects this and returns 'Universe.Unchanged'
                && !universe.DisposeRequested)
            {
                // perform initial filtering and limit the result
                selectSymbolsResult = universe.SelectSymbols(dateTimeUtc, universeData);

                if (!ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
                {
                    // prepare a BaseDataCollection of FineFundamental instances
                    var fineCollection = new BaseDataCollection();

                    // if the input is already fundamental data we just need to filter it and pass it through
                    var hasFundamentalData = universeData.Data.Count > 0 && universeData.Data[0] is Fundamental;
                    if(hasFundamentalData)
                    {
                        // Remove selected symbols that does not have fine fundamental data
                        var anyDoesNotHaveFundamentalData = false;

                        // only pre filter selected symbols if there actually is any fundamental data. This way we can support custom universe filtered by fine fundamental data
                        // which do not use coarse data as underlying, in which case it could happen that we try to load fine fundamental data that is missing, but no problem,
                        // 'FineFundamentalSubscriptionEnumeratorFactory' won't emit it
                        var set = selectSymbolsResult.ToHashSet();
                        fineCollection.Data.AddRange(universeData.Data.OfType<Fundamental>().Where(fundamental => {
                            // we remove to we distict by symbol
                            if (set.Remove(fundamental.Symbol))
                            {
                                if (!fundamental.HasFundamentalData)
                                {
                                    anyDoesNotHaveFundamentalData = true;
                                    return false;
                                }
                                return true;
                            }
                            return false;
                        }));

                        if (!_anyDoesNotHaveFundamentalDataWarningLogged && anyDoesNotHaveFundamentalData)
                        {
                            _algorithm.Debug("Note: Your coarse selection filter was updated to exclude symbols without fine fundamental data. Make sure your coarse filter excludes symbols where HasFundamental is false.");
                            _anyDoesNotHaveFundamentalDataWarningLogged = true;
                        }
                    }
                    else
                    {
                        // we need to load the fundamental data
                        var currentTime = dateTimeUtc.ConvertFromUtc(TimeZones.NewYork);
                        foreach (var symbol in selectSymbolsResult)
                        {
                            fineCollection.Data.Add(new Fundamental(currentTime, symbol));
                        }
                    }

                    universeData.Data = fineCollection.Data;
                    // perform the fine fundamental universe selection
                    selectSymbolsResult = fineFiltered.PerformSelection(dateTimeUtc, fineCollection);
                }
            }
            else
            {
                // perform initial filtering and limit the result
                selectSymbolsResult = universe.PerformSelection(dateTimeUtc, universeData);
            }

            if (!ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
            {
                // materialize the enumerable into a set for processing
                universe.Selected = selectSymbolsResult.ToHashSet();
            }

            // first check for no pending removals, even if the universe selection
            // didn't change we might need to remove a security because a position was closed
            RemoveSecurityFromUniverse(
                _pendingRemovalsManager.CheckPendingRemovals(universe.Selected, universe),
                dateTimeUtc,
                algorithmEndDateUtc);

            // check for no changes second
            if (ReferenceEquals(selectSymbolsResult, Universe.Unchanged))
            {
                return SecurityChanges.None;
            }

            // determine which data subscriptions need to be removed from this universe
            foreach (var member in universe.Securities.Values.OrderBy(member => member.Security.Symbol.SecurityType).ThenBy(x => x.Security.Symbol.ID))
            {
                var security = member.Security;
                // if we've selected this subscription again, keep it
                if (universe.Selected.Contains(security.Symbol)) continue;

                // don't remove if the universe wants to keep him in
                if (!universe.CanRemoveMember(dateTimeUtc, security)) continue;

                if (!member.Security.IsDelisted)
                {
                    // TODO: here we are not checking if other universes have this security still selected
                    _securityChangesConstructor.Remove(member.Security, member.IsInternal);
                }

                RemoveSecurityFromUniverse(_pendingRemovalsManager.TryRemoveMember(security, universe),
                    dateTimeUtc,
                    algorithmEndDateUtc);
            }

            Dictionary<Symbol, Security> pendingAdditions;
            if (!_pendingSecurityAdditions.TryGetValue(dateTimeUtc, out pendingAdditions))
            {
                // if the frontier moved forward then we've added these securities to the algorithm
                _pendingSecurityAdditions.Clear();

                // keep track of created securities so we don't create the same security twice, leads to bad things :)
                pendingAdditions = new Dictionary<Symbol, Security>();
                _pendingSecurityAdditions[dateTimeUtc] = pendingAdditions;
            }

            // find new selections and add them to the algorithm
            foreach (var symbol in universe.Selected)
            {
                if (universe.Securities.ContainsKey(symbol))
                {
                    // if its already part of the universe no need to re add it
                    continue;
                }

                Security underlying = null;
                if (symbol.HasUnderlying)
                {
                    underlying = GetOrCreateSecurity(pendingAdditions, symbol.Underlying, universe.UniverseSettings);
                }
                // create the new security, the algorithm thread will add this at the appropriate time
                var security = GetOrCreateSecurity(pendingAdditions, symbol, universe.UniverseSettings, underlying);

                var addedSubscription = false;
                var dataFeedAdded = false;
                var internalFeed = true;
                foreach (var request in universe.GetSubscriptionRequests(security, dateTimeUtc, algorithmEndDateUtc,
                                                                         _algorithm.SubscriptionManager.SubscriptionDataConfigService))
                {
                    if (!request.TradableDaysInDataTimeZone.Any())
                    {
                        // Remove the config from the data manager. universe.GetSubscriptionRequests() might have added the configs
                        _dataManager.RemoveSubscription(request.Configuration, universe);
                        continue;
                    }

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
                        // if any config isn't internal then it's not internal
                        internalFeed &= request.Configuration.IsInternalFeed;
                        _internalSubscriptionManager.AddedSubscriptionRequest(request);
                    }
                }

                if (addedSubscription)
                {
                    var addedMember = universe.AddMember(dateTimeUtc, security, internalFeed);

                    if (addedMember && dataFeedAdded)
                    {
                        _securityChangesConstructor.Add(security, internalFeed);
                    }
                }
            }

            var securityChanges = _securityChangesConstructor.Flush();

            // Add currency data feeds that weren't explicitly added in Initialize
            if (securityChanges.AddedSecurities.Count > 0)
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
                    var resolution = _algorithm.LiveMode ? Resolution.Minute : Resolution.Hour;

                    // Check that the tradebar subscription we are using can support this resolution GH #5893
                    var subscriptionType = _algorithm.SubscriptionManager.SubscriptionDataConfigService.LookupSubscriptionConfigDataTypes(securityBenchmark.Security.Type, resolution, securityBenchmark.Security.Symbol.IsCanonical()).First();
                    var symbol = securityBenchmark.Security.Symbol;
                    var isCustomData = false;

                    // Check if the benchmark security is a custom data in order to make sure we get the correct
                    // type
                    if (symbol.SecurityType == SecurityType.Base)
                    {
                        var symbolDataConfigs = _algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol);
                        if (symbolDataConfigs.Any())
                        {
                            subscriptionType = new Tuple<Type, TickType>(symbolDataConfigs.First().Type, TickType.Trade);
                            isCustomData = true;
                        }
                    }

                    var baseInstance = subscriptionType.Item1.GetBaseDataInstance();
                    baseInstance.Symbol = securityBenchmark.Security.Symbol;
                    var supportedResolutions = baseInstance.SupportedResolutions();
                    if (!supportedResolutions.Contains(resolution))
                    {
                        resolution = supportedResolutions.OrderByDescending(x => x).First();
                    }

                    var subscriptionList = new List<Tuple<Type, TickType>>() {subscriptionType};
                    var dataConfig = _algorithm.SubscriptionManager.SubscriptionDataConfigService.Add(
                        securityBenchmark.Security.Symbol,
                        resolution,
                        isInternalFeed: true,
                        fillForward: false,
                        isCustomData: isCustomData,
                        subscriptionDataTypes: subscriptionList
                        ).First();

                    // we want to start from the previous tradable bar so the benchmark security
                    // never has 0 price
                    var previousTradableBar = Time.GetStartTimeForTradeBars(
                        securityBenchmark.Security.Exchange.Hours,
                        utcStart.ConvertFromUtc(securityBenchmark.Security.Exchange.TimeZone),
                        _algorithm.LiveMode ? Time.OneMinute : Time.OneDay,
                        1,
                        false,
                        dataConfig.DataTimeZone,
                        LeanData.UseStrictEndTime(_algorithm.Settings.DailyPreciseEndTime, securityBenchmark.Security.Symbol, _algorithm.LiveMode ? Time.OneMinute : Time.OneDay, securityBenchmark.Security.Exchange.Hours)
                        ).ConvertToUtc(securityBenchmark.Security.Exchange.TimeZone);

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

        /// <summary>
        /// Handles the delisting process of the given data symbol from the algorithm securities
        /// </summary>
        public SecurityChanges HandleDelisting(BaseData data, bool isInternalFeed)
        {
            if (_algorithm.Securities.TryGetValue(data.Symbol, out var security))
            {
                // don't allow users to open a new position once delisted
                security.IsDelisted = true;
                security.Reset();

                // Add the security removal to the security changes but only if not pending for removal.
                // If pending, the removed change event was already emitted for this security
                if (_algorithm.Securities.Remove(data.Symbol) && !_pendingRemovalsManager.PendingRemovals.Values.Any(x => x.Any(y => y.Symbol == data.Symbol)))
                {
                    _securityChangesConstructor.Remove(security, isInternalFeed);

                    return _securityChangesConstructor.Flush();
                }
            }

            return SecurityChanges.None;
        }

        private void RemoveSecurityFromUniverse(
            List<PendingRemovalsManager.RemovedMember> removedMembers,
            DateTime dateTimeUtc,
            DateTime algorithmEndDateUtc)
        {
            if (removedMembers == null)
            {
                return;
            }
            foreach (var removedMember in removedMembers)
            {
                var universe = removedMember.Universe;
                var member = removedMember.Security;

                // safe to remove the member from the universe
                universe.RemoveMember(dateTimeUtc, member);

                var isActive = _algorithm.UniverseManager.ActiveSecurities.ContainsKey(member.Symbol);
                foreach (var subscription in universe.GetSubscriptionRequests(member, dateTimeUtc, algorithmEndDateUtc,
                                                                              _algorithm.SubscriptionManager.SubscriptionDataConfigService))
                {
                    if (_dataManager.RemoveSubscription(subscription.Configuration, universe))
                    {
                        _internalSubscriptionManager.RemovedSubscriptionRequest(subscription);

                        // if not used by any universe
                        if (!isActive)
                        {
                            member.Reset();
                            // We need to mark this security as untradeable while it has no data subscription
                            // it is expected that this function is called while in sync with the algo thread,
                            // so we can make direct edits to the security here.
                            // We only clear the cache once the subscription is removed from the data stack
                            // Note: Security.Reset() won't clear the cache, it only clears the data subscription
                            // and marks it as non-tradable, because in some cases the cache needs to be kept,
                            // like when delisting, which could lead to a liquidation or option exercise.
                            member.Cache.Reset();

                            _algorithm.Securities.Remove(member.Symbol);
                        }
                    }
                }
            }
        }

        private Security GetOrCreateSecurity(Dictionary<Symbol, Security> pendingAdditions, Symbol symbol, UniverseSettings universeSettings, Security underlying = null)
        {
            // create the new security, the algorithm thread will add this at the appropriate time
            Security security;
            if (!pendingAdditions.TryGetValue(symbol, out security))
            {
                security = _securityService.CreateSecurity(symbol, new List<SubscriptionDataConfig>(), universeSettings.Leverage, symbol.ID.SecurityType.IsOption(), underlying);

                pendingAdditions.Add(symbol, security);
            }

            return security;
        }
    }
}
