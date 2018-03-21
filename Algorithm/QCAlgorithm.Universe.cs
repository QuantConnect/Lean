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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
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
        private readonly Dictionary<Security, UserDefinedUniverse> _pendingUserDefinedUniverseSecurityAdditions = new Dictionary<Security, UserDefinedUniverse>();
        private readonly List<Universe> _pendingUniverseAdditions = new List<Universe>();

        /// <summary>
        /// Gets universe manager which holds universes keyed by their symbol
        /// </summary>
        public UniverseManager UniverseManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the universe settings to be used when adding securities via universe selection
        /// </summary>
        public UniverseSettings UniverseSettings
        {
            get;
            private set;
        }

        /// <summary>
        /// Invoked at the end of every time step. This allows the algorithm
        /// to process events before advancing to the next time step.
        /// </summary>
        public void OnEndOfTimeStep()
        {
            if (_pendingUniverseAdditions.Count + _pendingUserDefinedUniverseSecurityAdditions.Count == 0)
            {
                // no point in looping through everything if there's no pending changes
                return;
            }

            // rewrite securities w/ derivatives to be in raw mode
            lock (_pendingUniverseAdditionsLock)
            {

                foreach (var security in Securities.Select(kvp => kvp.Value).Union(_pendingUserDefinedUniverseSecurityAdditions.Keys))
                {
                    // check for any derivative securities and mark the underlying as raw
                    if (Securities.Any(skvp => skvp.Key.HasUnderlyingSymbol(security.Symbol)))
                    {
                        // set data mode raw and default volatility model
                        ConfigureUnderlyingSecurity(security);
                    }

                    if (security.Symbol.HasUnderlying)
                    {
                        Security underlyingSecurity;
                        var underlyingSymbol = security.Symbol.Underlying;

                        // create the underlying security object if it doesn't already exist
                        if (!Securities.TryGetValue(underlyingSymbol, out underlyingSecurity))
                        {
                            underlyingSecurity = AddSecurity(underlyingSymbol.SecurityType, underlyingSymbol.Value, security.Resolution,
                                underlyingSymbol.ID.Market, false, 0, security.IsExtendedMarketHours);
                        }

                        // set data mode raw and default volatility model
                        ConfigureUnderlyingSecurity(underlyingSecurity);

                        // set the underying security on the derivative -- we do this in two places since it's possible
                        // to do AddOptionContract w/out the underlying already added and normalized properly
                        var derivative = security as IDerivativeSecurity;
                        if (derivative != null)
                        {
                            derivative.Underlying = underlyingSecurity;
                        }
                    }
                }

                // add securities to their respective user defined universes
                foreach (var kvp in _pendingUserDefinedUniverseSecurityAdditions)
                {
                    var security = kvp.Key;
                    var userDefinedUniverse = kvp.Value;
                    userDefinedUniverse.Add(security.Symbol);
                }

                // finally add any pending universes, this will make them available to the data feed
                foreach (var universe in _pendingUniverseAdditions)
                {
                    UniverseManager.Add(universe.Configuration.Symbol, universe);
                }

                _pendingUniverseAdditions.Clear();
                _pendingUserDefinedUniverseSecurityAdditions.Clear();
            }
        }

        /// <summary>
        /// Gets a helper that provides pre-defined universe defintions, such as top dollar volume
        /// </summary>
        public UniverseDefinitions Universe
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds the universe to the algorithm
        /// </summary>
        /// <param name="universe">The universe to be added</param>
        public void AddUniverse(Universe universe)
        {
            UniverseManager.Add(universe.Configuration.Symbol, universe);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(string name, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(SecurityType.Equity, name, Resolution.Daily, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Resolution.Daily, Market.USA, and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(string name, Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            AddUniverse(SecurityType.Equity, name, Resolution.Daily, Market.USA, UniverseSettings, selector);
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
        public void AddUniverse<T>(string name, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(SecurityType.Equity, name, Resolution.Daily, Market.USA, universeSettings, selector);
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
        public void AddUniverse<T>(string name, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            AddUniverse(SecurityType.Equity, name, Resolution.Daily, Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Market.USA and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(string name, Resolution resolution, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(SecurityType.Equity, name, resolution, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, Market.USA and UniverseSettings
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(string name, Resolution resolution, Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            AddUniverse(SecurityType.Equity, name, resolution, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, and Market.USA
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(string name, Resolution resolution, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(SecurityType.Equity, name, resolution, Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property. This universe will use the defaults
        /// of SecurityType.Equity, and Market.USA
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="universeSettings">The settings used for securities added by this universe</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(string name, Resolution resolution, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            AddUniverse(SecurityType.Equity, name, resolution, Market.USA, universeSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(SecurityType securityType, string name, Resolution resolution, string market, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(securityType, name, resolution, market, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(SecurityType securityType, string name, Resolution resolution, string market, Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            AddUniverse(securityType, name, resolution, market, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            var marketHoursDbEntry = MarketHoursDatabase.GetEntry(market, name, securityType);
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            var symbol = QuantConnect.Symbol.Create(name, securityType, market);
            var config = new SubscriptionDataConfig(typeof(T), symbol, resolution, dataTimeZone, exchangeTimeZone, false, false, true, true, isFilteredSubscription: false);
            AddUniverse(new FuncUniverse(config, universeSettings, SecurityInitializer, d => selector(d.OfType<T>())));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="universeSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<string>> selector)
        {
            var marketHoursDbEntry = MarketHoursDatabase.GetEntry(market, name, securityType);
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            var symbol = QuantConnect.Symbol.Create(name, securityType, market);
            var config = new SubscriptionDataConfig(typeof(T), symbol, resolution, dataTimeZone, exchangeTimeZone, false, false, true, true, isFilteredSubscription: false);
            AddUniverse(new FuncUniverse(config, universeSettings, SecurityInitializer, d => selector(d.OfType<T>()).Select(x => QuantConnect.Symbol.Create(x, securityType, market))));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="selector">Defines an initial coarse selection</param>
        public void AddUniverse(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(new CoarseFundamentalUniverse(UniverseSettings, SecurityInitializer, selector));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for coarse and fine fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="coarseSelector">Defines an initial coarse selection</param>
        /// <param name="fineSelector">Defines a more detailed selection with access to more data</param>
        public void AddUniverse(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector, Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> fineSelector)
        {
            var coarse = new CoarseFundamentalUniverse(UniverseSettings, SecurityInitializer, coarseSelector);

            AddUniverse(new FineFundamentalFilteredUniverse(coarse, fineSelector));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This is for fine fundamental US Equity data and
        /// will be executed on day changes in the NewYork time zone (<see cref="TimeZones.NewYork"/>
        /// </summary>
        /// <param name="universe">The universe to be filtered with fine fundamental selection</param>
        /// <param name="fineSelector">Defines a more detailed selection with access to more data</param>
        public void AddUniverse(Universe universe, Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> fineSelector)
        {
            AddUniverse(new FineFundamentalFilteredUniverse(universe, fineSelector));
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public void AddUniverse(string name, Func<DateTime, IEnumerable<string>> selector)
        {
            AddUniverse(SecurityType.Equity, name, Resolution.Daily, Market.USA, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This can be used to return a list of string
        /// symbols retrieved from anywhere and will loads those symbols under the US Equity market.
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="resolution">The resolution this universe should be triggered on</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public void AddUniverse(string name, Resolution resolution, Func<DateTime, IEnumerable<string>> selector)
        {
            AddUniverse(SecurityType.Equity, name, resolution, Market.USA, UniverseSettings, selector);
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
        public void AddUniverse(SecurityType securityType, string name, Resolution resolution, string market, UniverseSettings universeSettings, Func<DateTime, IEnumerable<string>> selector)
        {
            var marketHoursDbEntry = MarketHoursDatabase.GetEntry(market, name, securityType);
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            var symbol = QuantConnect.Symbol.Create(name, securityType, market);
            var config = new SubscriptionDataConfig(typeof(CoarseFundamental), symbol, resolution, dataTimeZone, exchangeTimeZone, false, false, true, isFilteredSubscription: false);
            AddUniverse(new UserDefinedUniverse(config, universeSettings, SecurityInitializer, resolution.ToTimeSpan(), selector));
        }

        /// <summary>
        /// Adds the security to the user defined universe for the specified
        /// </summary>
        private void AddToUserDefinedUniverse(Security security)
        {
            // if we are adding a non-internal security which already has an internal feed, we remove it first
            Security existingSecurity;
            if (Securities.TryGetValue(security.Symbol, out existingSecurity))
            {
                if (!security.IsInternalFeed() && existingSecurity.IsInternalFeed())
                {
                    var securityUniverse = UniverseManager.Select(x => x.Value).OfType<UserDefinedUniverse>().FirstOrDefault(x => x.Members.ContainsKey(security.Symbol));
                    securityUniverse?.Remove(security.Symbol);

                    Securities.Remove(security.Symbol);
                }
            }

            Securities.Add(security);

            // add this security to the user defined universe
            Universe universe;
            var subscription = security.Subscriptions.First();
            var universeSymbol = UserDefinedUniverse.CreateSymbol(subscription.SecurityType, subscription.Market);
            lock (_pendingUniverseAdditionsLock)
            {
                if (!UniverseManager.TryGetValue(universeSymbol, out universe))
                {
                    universe = _pendingUniverseAdditions.FirstOrDefault(x => x.Configuration.Symbol == universeSymbol);
                    if (universe == null)
                    {
                        // create a new universe, these subscription settings don't currently get used
                        // since universe selection proper is never invoked on this type of universe
                        var uconfig = new SubscriptionDataConfig(subscription, symbol: universeSymbol, isInternalFeed: true, fillForward: false);

                        if (security.Type == SecurityType.Base)
                        {
                            // set entry in market hours database for the universe subscription to match the custom data
                            var symbolString = MarketHoursDatabase.GetDatabaseSymbolKey(uconfig.Symbol);
                            MarketHoursDatabase.SetEntry(uconfig.Market, symbolString, uconfig.SecurityType, security.Exchange.Hours, uconfig.DataTimeZone);
                        }

                        universe = new UserDefinedUniverse(uconfig,
                            new UniverseSettings(security.Resolution, security.Leverage, security.IsFillDataForward, security.IsExtendedMarketHours,
                                TimeSpan.Zero),
                            SecurityInitializer,
                            QuantConnect.Time.MaxTimeSpan,
                            new List<Symbol> {security.Symbol}
                        );
                        _pendingUniverseAdditions.Add(universe);
                    }
                }
            }

            var userDefinedUniverse = universe as UserDefinedUniverse;
            if (userDefinedUniverse != null)
            {
                lock (_pendingUniverseAdditionsLock)
                {
                    _pendingUserDefinedUniverseSecurityAdditions.Add(security, userDefinedUniverse);
                }
            }
            else
            {
                // should never happen, someone would need to add a non-user defined universe with this symbol
                throw new Exception("Expected universe with symbol '" + universeSymbol.Value + "' to be of type UserDefinedUniverse.");
            }
        }

        /// <summary>
        /// Configures the security to be in raw data mode and ensures that a reasonable default volatility model is supplied
        /// </summary>
        /// <param name="security">The underlying security</param>
        private void ConfigureUnderlyingSecurity(Security security)
        {
            // force underlying securities to be raw data mode
            if (security.DataNormalizationMode != DataNormalizationMode.Raw)
            {
                Debug($"Warning: The {security.Symbol.Value} equity security was set the raw price normalization mode to work with options.");
                security.SetDataNormalizationMode(DataNormalizationMode.Raw);
            }

            // ensure a volatility model has been set on the underlying
            if (security.VolatilityModel == VolatilityModel.Null)
            {
                security.VolatilityModel = new StandardDeviationOfReturnsVolatilityModel(periods: 30);
            }
        }
    }
}
