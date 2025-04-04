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
using NodaTime;
using QuantConnect.Algorithm.Selection;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        // save universe additions and apply at end of time step
        // this removes temporal dependencies from w/in initialize method
        // original motivation: adding equity/options to enforce equity raw data mode
        private readonly object _pendingUniverseAdditionsLock = new object();
        private readonly List<UserDefinedUniverseAddition> _pendingUserDefinedUniverseSecurityAdditions = new List<UserDefinedUniverseAddition>();
        private bool _pendingUniverseAdditions;
        private ConcurrentSet<Symbol> _rawNormalizationWarningSymbols = new ConcurrentSet<Symbol>();
        private readonly int _rawNormalizationWarningSymbolsMaxCount = 10;

        /// <summary>
        /// Gets universe manager which holds universes keyed by their symbol
        /// </summary>
        [DocumentationAttribute(Universes)]
        public UniverseManager UniverseManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the universe settings to be used when adding securities via universe selection
        /// </summary>
        [DocumentationAttribute(Universes)]
        public UniverseSettings UniverseSettings
        {
            get;
            private set;
        }

        /// <summary>
        /// Invoked at the end of every time step. This allows the algorithm
        /// to process events before advancing to the next time step.
        /// </summary>
        [DocumentationAttribute(HandlingData)]
        public void OnEndOfTimeStep()
        {
            // rewrite securities w/ derivatives to be in raw mode
            lock (_pendingUniverseAdditionsLock)
            {
                if (!_pendingUniverseAdditions && _pendingUserDefinedUniverseSecurityAdditions.Count == 0)
                {
                    // no point in looping through everything if there's no pending changes
                    return;
                }

                var requiredHistoryRequests = new Dictionary<Security, Resolution>();

                foreach (var security in Securities.Select(kvp => kvp.Value).Union(
                    _pendingUserDefinedUniverseSecurityAdditions.Select(x => x.Security)))
                {
                    // check for any derivative securities and mark the underlying as raw
                    if (Securities.Any(skvp => skvp.Key.SecurityType != SecurityType.Base && skvp.Key.HasUnderlyingSymbol(security.Symbol)))
                    {
                        // set data mode raw and default volatility model
                        ConfigureUnderlyingSecurity(security);
                    }

                    var configs = SubscriptionManager.SubscriptionDataConfigService
                        .GetSubscriptionDataConfigs(security.Symbol);
                    if (security.Symbol.HasUnderlying && security.Symbol.SecurityType != SecurityType.Base)
                    {
                        Security underlyingSecurity;
                        var underlyingSymbol = security.Symbol.Underlying;

                        var resolution = configs.GetHighestResolution();
                        var isFillForward = configs.IsFillForward();
                        if (UniverseManager.TryGetValue(security.Symbol, out var universe))
                        {
                            // as if the universe had selected this asset, the configuration of the canonical can be different
                            resolution = universe.UniverseSettings.Resolution;
                            isFillForward = universe.UniverseSettings.FillForward;
                        }

                        // create the underlying security object if it doesn't already exist
                        if (!Securities.TryGetValue(underlyingSymbol, out underlyingSecurity))
                        {
                            underlyingSecurity = AddSecurity(underlyingSymbol.SecurityType,
                                underlyingSymbol.Value,
                                resolution,
                                underlyingSymbol.ID.Market,
                                isFillForward,
                                Security.NullLeverage,
                                configs.IsExtendedMarketHours(),
                                dataNormalizationMode: DataNormalizationMode.Raw);
                        }

                        // set data mode raw and default volatility model
                        ConfigureUnderlyingSecurity(underlyingSecurity);

                        if (LiveMode && underlyingSecurity.GetLastData() == null)
                        {
                            if (requiredHistoryRequests.ContainsKey(underlyingSecurity))
                            {
                                // lets request the higher resolution
                                var currentResolutionRequest = requiredHistoryRequests[underlyingSecurity];
                                if (currentResolutionRequest != Resolution.Minute  // Can not be less than Minute
                                    && resolution < currentResolutionRequest)
                                {
                                    requiredHistoryRequests[underlyingSecurity] = (Resolution)Math.Max((int)resolution, (int)Resolution.Minute);
                                }
                            }
                            else
                            {
                                requiredHistoryRequests.Add(underlyingSecurity, (Resolution)Math.Max((int)resolution, (int)Resolution.Minute));
                            }
                        }
                        // set the underlying security on the derivative -- we do this in two places since it's possible
                        // to do AddOptionContract w/out the underlying already added and normalized properly
                        var derivative = security as IDerivativeSecurity;
                        if (derivative != null)
                        {
                            derivative.Underlying = underlyingSecurity;
                        }
                    }
                }

                if (!requiredHistoryRequests.IsNullOrEmpty())
                {
                    // Create requests
                    var historyRequests = Enumerable.Empty<HistoryRequest>();
                    foreach (var byResolution in requiredHistoryRequests.GroupBy(x => x.Value))
                    {
                        historyRequests = historyRequests.Concat(
                            CreateBarCountHistoryRequests(byResolution.Select(x => x.Key.Symbol), 3, byResolution.Key));
                    }
                    // Request data
                    var historicLastData = History(historyRequests);
                    historicLastData.PushThrough(x =>
                    {
                        var security = requiredHistoryRequests.Keys.FirstOrDefault(y => y.Symbol == x.Symbol);
                        security?.Cache.AddData(x);
                    });
                }

                // add subscriptionDataConfig to their respective user defined universes
                foreach (var userDefinedUniverseAddition in _pendingUserDefinedUniverseSecurityAdditions)
                {
                    foreach (var subscriptionDataConfig in userDefinedUniverseAddition.SubscriptionDataConfigs)
                    {
                        userDefinedUniverseAddition.Universe.Add(subscriptionDataConfig);
                    }
                }

                // finally add any pending universes, this will make them available to the data feed
                // The universe will be added at the end of time step, same as the AddData user defined universes.
                // This is required to be independent of the start and end date set during initialize
                UniverseManager.ProcessChanges();

                _pendingUniverseAdditions = false;
                _pendingUserDefinedUniverseSecurityAdditions.Clear();
            }

            if (!_rawNormalizationWarningSymbols.IsNullOrEmpty())
            {
                // Log our securities being set to raw price mode
                Debug($"Warning: The following securities were set to raw price normalization mode to work with options: " +
                    $"{string.Join(", ", _rawNormalizationWarningSymbols.Take(_rawNormalizationWarningSymbolsMaxCount).Select(x => x.Value))}...");

                // Set our warning list to null to stop emitting these warnings after its done once
                _rawNormalizationWarningSymbols = null;
            }
        }

        /// <summary>
        /// Gets a helper that provides pre-defined universe definitions, such as top dollar volume
        /// </summary>
        [DocumentationAttribute(Universes)]
        public UniverseDefinitions Universe
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds the universe to the algorithm
        /// </summary>
        /// <param name="universe">The universe to be added</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(Universe universe)
        {
            if (universe.UniverseSettings == null)
            {
                // set default value so that users don't need to pass it
                universe.UniverseSettings = UniverseSettings;
            }
            _pendingUniverseAdditions = true;
            // note: UniverseManager.Add uses TryAdd, so don't need to worry about duplicates here
            UniverseManager.Add(universe.Configuration.Symbol, universe);
            return universe;
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse<T>(null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverse<T>(null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse<T>(name, null, null, null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverseStringSelector<T>(selector, null, name);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, and Market.USA
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, UniverseSettings universeSettings, Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse<T>(name, null, null, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, and Market.USA
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, UniverseSettings universeSettings, Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverseStringSelector<T>(selector, null, name, null, null, universeSettings);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Market.USA and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Resolution resolution, Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse<T>(name, resolution, null, null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Market.USA and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Resolution resolution, Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverseStringSelector<T>(selector, null, name, resolution);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, and Market.USA
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Resolution resolution, UniverseSettings universeSettings, Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse<T>(name, resolution, market: null, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, and Market.USA
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Resolution resolution, UniverseSettings universeSettings, Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverseStringSelector<T>(selector, null, name, resolution, universeSettings: universeSettings);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name, Resolution resolution, string market, Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse<T>(name, resolution, market, null, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(SecurityType securityType, string name, Resolution resolution, string market, Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverseStringSelector<T>(selector, securityType, name, resolution, market);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, Func<IEnumerable<BaseData>, IEnumerable<string>> selector)
        {
            return AddUniverseStringSelector<T>(selector, securityType, name, resolution, market, universeSettings);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The expected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse<T>(string name = null, Resolution? resolution = null, string market = null, UniverseSettings universeSettings = null,
            Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector = null)
        {
            return AddUniverseSymbolSelector(typeof(T), name, resolution, market, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>)
        /// </summary>
        /// <param name="selector">Defines an initial coarse selection</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> selector)
        {
            return AddUniverse(FundamentalUniverse.USA(selector));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse fundamental US Equity data and
        /// will be executed based on the provided <see cref="IDateRule"/> in the NewYork time zone (<see cref="TimeZones.NewYork"/>)
        /// </summary>
        /// <param name="dateRule">Date rule that will be used to set the <see cref="Data.UniverseSelection.UniverseSettings.Schedule"/></param>
        /// <param name="selector">Defines an initial coarse selection</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(IDateRule dateRule, Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> selector)
        {
            var otherSettings = new UniverseSettings(UniverseSettings);
            otherSettings.Schedule.On(dateRule);
            return AddUniverse(FundamentalUniverse.USA(selector, otherSettings));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse and fine fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>)
        /// </summary>
        /// <param name="coarseSelector">Defines an initial coarse selection</param>
        /// <param name="fineSelector">Defines a more detailed selection with access to more data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector, Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> fineSelector)
        {
            var coarse = new CoarseFundamentalUniverse(UniverseSettings, coarseSelector);

            return AddUniverse(new FineFundamentalFilteredUniverse(coarse, fineSelector));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for fine fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>)
        /// </summary>
        /// <param name="universe">The universe to be filtered with fine fundamental selection</param>
        /// <param name="fineSelector">Defines a more detailed selection with access to more data</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(Universe universe, Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> fineSelector)
        {
            return AddUniverse(new FundamentalFilteredUniverse(universe, fineSelector));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(string name, Func<DateTime, IEnumerable<string>> selector)
        {
            return AddUniverse(SecurityType.Equity, name, Resolution.Daily, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The resolution this universe should be triggered on</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(string name, Resolution resolution, Func<DateTime, IEnumerable<string>> selector)
        {
            return AddUniverse(SecurityType.Equity, name, resolution, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new user defined universe that will fire on the requested resolution during market hours.
        /// </summary>
        /// <param name="securityType">The security type of the universe</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The resolution this universe should be triggered on</param>
        /// <param name="market">The market of the universe</param>
        /// <param name="universeSettings">The subscription settings used for securities added from this universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        [DocumentationAttribute(Universes)]
        public Universe AddUniverse(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, Func<DateTime, IEnumerable<string>> selector)
        {
            var marketHoursDbEntry = MarketHoursDatabase.GetEntry(market, name, securityType);
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            var symbol = QuantConnect.Symbol.Create(name, securityType, market);
            var config = new SubscriptionDataConfig(typeof(Fundamental), symbol, resolution, dataTimeZone, exchangeTimeZone, false, false, true, isFilteredSubscription: false);
            return AddUniverse(new UserDefinedUniverse(config, universeSettings, resolution.ToTimeSpan(), selector));
        }

        /// <summary>
        /// Adds a new universe that creates options of the security by monitoring any changes in the Universe the provided security is in.
        /// Additionally, a filter can be applied to the options generated when the universe of the security changes.
        /// </summary>
        /// <param name="underlyingSymbol">Underlying Symbol to add as an option. For Futures, the option chain constructed will be per-contract, as long as a canonical Symbol is provided.</param>
        /// <param name="optionFilter">User-defined filter used to select the options we want out of the option chain provided.</param>
        /// <exception cref="InvalidOperationException">The underlying Symbol's universe is not found.</exception>
        [DocumentationAttribute(Universes)]
        public void AddUniverseOptions(Symbol underlyingSymbol, Func<OptionFilterUniverse, OptionFilterUniverse> optionFilter)
        {
            // We need to load the universe associated with the provided Symbol and provide that universe to the option filter universe.
            // The option filter universe will subscribe to any changes in the universe of the underlying Symbol,
            // ensuring that we load the option chain for every asset found in the underlying's Universe.
            Universe universe;
            if (!UniverseManager.TryGetValue(underlyingSymbol, out universe))
            {
                underlyingSymbol = AddSecurity(underlyingSymbol).Symbol;

                // Recheck again, we should have a universe addition pending for the provided Symbol
                if (!UniverseManager.TryGetValue(underlyingSymbol, out universe))
                {
                    // Should never happen, but it could be that the subscription
                    // created with AddSecurity is not aligned with the Symbol we're using.
                    throw new InvalidOperationException($"Universe not found for underlying Symbol: {underlyingSymbol}.");
                }
            }

            // Allow all option contracts through without filtering if we're provided a null filter.
            AddUniverseOptions(universe, optionFilter ?? (_ => _));
        }

        /// <summary>
        /// Creates a new universe selection model and adds it to the algorithm. This universe selection model will chain to the security
        /// changes of a given <see cref="Universe"/> selection output and create a new <see cref="OptionChainUniverse"/> for each of them
        /// </summary>
        /// <param name="universe">The universe we want to chain an option universe selection model too</param>
        /// <param name="optionFilter">The option filter universe to use</param>
        [DocumentationAttribute(Universes)]
        public void AddUniverseOptions(Universe universe, Func<OptionFilterUniverse, OptionFilterUniverse> optionFilter)
        {
            AddUniverseSelection(new OptionChainedUniverseSelectionModel(universe, optionFilter));
        }

        /// <summary>
        /// Adds the security to the user defined universe
        /// </summary>
        /// <param name="security">The security to add</param>
        /// <param name="configurations">The <see cref="SubscriptionDataConfig"/> instances we want to add</param>
        private Security AddToUserDefinedUniverse(
            Security security,
            List<SubscriptionDataConfig> configurations)
        {
            var subscription = configurations.First();
            // if we are adding a non-internal security which already has an internal feed, we remove it first
            if (Securities.TryGetValue(security.Symbol, out var existingSecurity))
            {
                if (!subscription.IsInternalFeed && existingSecurity.IsInternalFeed())
                {
                    var securityUniverse = UniverseManager.Select(x => x.Value).OfType<UserDefinedUniverse>().FirstOrDefault(x => x.Members.ContainsKey(security.Symbol));
                    securityUniverse?.Remove(security.Symbol);

                    Securities.Remove(security.Symbol);
                    Securities.Add(security);
                }
            }
            else
            {
                Securities.Add(security);
            }

            // add this security to the user defined universe
            Universe universe;
            var universeSymbol = UserDefinedUniverse.CreateSymbol(security.Type, security.Symbol.ID.Market);
            if (!UniverseManager.TryGetValue(universeSymbol, out universe))
            {
                if (universe == null)
                {
                    // create a new universe, these subscription settings don't currently get used
                    // since universe selection proper is never invoked on this type of universe
                    var uconfig = new SubscriptionDataConfig(subscription, symbol: universeSymbol, isInternalFeed: true, fillForward: false,
                        exchangeTimeZone: DateTimeZone.Utc,
                        dataTimeZone: DateTimeZone.Utc);

                    // this is the universe symbol, has no real entry in the mhdb, will default to market and security type
                    // set entry in market hours database for the universe subscription to match the config
                    var symbolString = MarketHoursDatabase.GetDatabaseSymbolKey(uconfig.Symbol);
                    MarketHoursDatabase.SetEntry(uconfig.Market, symbolString, uconfig.SecurityType,
                        SecurityExchangeHours.AlwaysOpen(uconfig.ExchangeTimeZone), uconfig.DataTimeZone);

                    universe = new UserDefinedUniverse(uconfig,
                        new UniverseSettings(
                            subscription.Resolution,
                            security.Leverage,
                            subscription.FillDataForward,
                            subscription.ExtendedMarketHours,
                            TimeSpan.Zero),
                        QuantConnect.Time.MaxTimeSpan,
                        new List<Symbol>());

                    AddUniverse(universe);
                }
            }

            var userDefinedUniverse = universe as UserDefinedUniverse;
            if (userDefinedUniverse != null)
            {
                lock (_pendingUniverseAdditionsLock)
                {
                    _pendingUserDefinedUniverseSecurityAdditions.Add(
                        new UserDefinedUniverseAddition(userDefinedUniverse, configurations, security));
                }
            }
            else
            {
                // should never happen, someone would need to add a non-user defined universe with this symbol
                throw new Exception($"Expected universe with symbol '{universeSymbol.Value}' to be of type {nameof(UserDefinedUniverse)} but was {universe.GetType().Name}.");
            }

            return security;
        }

        /// <summary>
        /// Configures the security to be in raw data mode and ensures that a reasonable default volatility model is supplied
        /// </summary>
        /// <param name="security">The underlying security</param>
        private void ConfigureUnderlyingSecurity(Security security)
        {
            // force underlying securities to be raw data mode
            var configs = SubscriptionManager.SubscriptionDataConfigService
                .GetSubscriptionDataConfigs(security.Symbol);
            if (configs.DataNormalizationMode() != DataNormalizationMode.Raw)
            {
                // Add this symbol to our set of raw normalization warning symbols to alert the user at the end
                // Set a hard limit to avoid growing this collection unnecessarily large
                if (_rawNormalizationWarningSymbols != null && _rawNormalizationWarningSymbols.Count <= _rawNormalizationWarningSymbolsMaxCount)
                {
                    _rawNormalizationWarningSymbols.Add(security.Symbol);
                }

                configs.SetDataNormalizationMode(DataNormalizationMode.Raw);
                // For backward compatibility we need to refresh the security DataNormalizationMode Property
                security.RefreshDataNormalizationModeProperty();
            }
        }

        /// <summary>
        /// Helper method to create the configuration of a custom universe
        /// </summary>
        private SubscriptionDataConfig GetCustomUniverseConfiguration(Type dataType, string name, string market, Resolution? resolution = null)
        {
            if (dataType == typeof(CoarseFundamental) || dataType == typeof(FineFundamental))
            {
                dataType = typeof(FundamentalUniverse);
            }

            if (!TryGetUniverseSymbol(dataType, market, out var universeSymbol))
            {
                market ??= Market.USA;
                if (string.IsNullOrEmpty(name))
                {
                    name = $"{dataType.Name}-{market}-{Guid.NewGuid()}";
                }
                // same as 'AddData<>' 'T' type will be treated as custom/base data type with always open market hours
                universeSymbol = QuantConnect.Symbol.Create(name, SecurityType.Base, market, baseDataType: dataType);
            }
            var marketHoursDbEntry = MarketHoursDatabase.GetEntry(universeSymbol, new[] { dataType });
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            return new SubscriptionDataConfig(dataType, universeSymbol, resolution ?? Resolution.Daily, dataTimeZone, exchangeTimeZone, false, false, true, true, isFilteredSubscription: false);
        }

        private bool TryGetUniverseSymbol(Type dataType, string market, out Symbol symbol)
        {
            symbol = null;
            if (dataType.IsAssignableTo(typeof(BaseDataCollection)))
            {
                var instance = dataType.GetBaseDataInstance() as BaseDataCollection;
                symbol = instance.UniverseSymbol(market);
                return true;
            }
            return false;
        }

        private Universe AddUniverseStringSelector<T>(Func<IEnumerable<BaseData>, IEnumerable<string>> selector, SecurityType? securityType = null, string name = null,
            Resolution? resolution = null, string market = null, UniverseSettings universeSettings = null)
        {
            if (market.IsNullOrEmpty())
            {
                market = Market.USA;
            }
            securityType ??= SecurityType.Equity;
            Func<IEnumerable<BaseData>, IEnumerable<Symbol>> symbolSelector = data => selector(data).Select(x => QuantConnect.Symbol.Create(x, securityType.Value, market, baseDataType: typeof(T)));
            return AddUniverse<T>(name, resolution, market, universeSettings, symbolSelector);
        }

        private Universe AddUniverseSymbolSelector(Type dataType, string name = null, Resolution? resolution = null, string market = null, UniverseSettings universeSettings = null,
            Func<IEnumerable<BaseData>, IEnumerable<Symbol>> selector = null)
        {
            var config = GetCustomUniverseConfiguration(dataType, name, market, resolution);
            return AddUniverse(new FuncUniverse(config, universeSettings, selector));
        }

        /// <summary>
        /// Helper class used to store <see cref="UserDefinedUniverse"/> additions.
        /// They will be consumed at <see cref="OnEndOfTimeStep"/>
        /// </summary>
        private class UserDefinedUniverseAddition
        {
            public Security Security { get; }
            public UserDefinedUniverse Universe { get; }
            public List<SubscriptionDataConfig> SubscriptionDataConfigs { get; }

            public UserDefinedUniverseAddition(
                UserDefinedUniverse universe,
                List<SubscriptionDataConfig> subscriptionDataConfigs,
                Security security)
            {
                Universe = universe;
                SubscriptionDataConfigs = subscriptionDataConfigs;
                Security = security;
            }
        }
    }
}
