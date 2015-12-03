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
using QuantConnect.Securities;
using SecurityTypeMarket = System.Tuple<QuantConnect.SecurityType, string>;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        // user defined universes by security type
        private readonly Dictionary<SecurityTypeMarket, UserDefinedUniverse> _userDefinedUniverses;

        /// <summary>
        /// Gets the current universe selector, or null if no selection is to be performed
        /// </summary>
        public List<Universe> Universes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the subscription settings to be used when adding securities via universe selection
        /// </summary>
        public SubscriptionSettings UniverseSettings
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
            Universes.Add(universe);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm. This will use the default universe settings
        /// specified via the <see cref="UniverseSettings"/> property.
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="symbol">The symbol for the universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(SecurityType securityType, string symbol, Resolution resolution, string market, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            AddUniverse(securityType, symbol, resolution, market, UniverseSettings, selector);
        }

        /// <summary>
        /// Creates a new universe and adds it to the algorithm
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="securityType">The security type the universe produces</param>
        /// <param name="symbol">The symbol for the universe</param>
        /// <param name="resolution">The epected resolution of the universe data</param>
        /// <param name="market">The market for selected symbols</param>
        /// <param name="subscriptionSettings">The subscription settings to use for newly created subscriptions</param>
        /// <param name="selector">Function delegate that performs selection on the universe data</param>
        public void AddUniverse<T>(SecurityType securityType, string symbol, Resolution resolution, string market, SubscriptionSettings subscriptionSettings, Func<IEnumerable<T>, IEnumerable<Symbol>> selector)
        {
            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(market, symbol, securityType);
            var dataTimeZone = marketHoursDbEntry.DataTimeZone;
            var exchangeTimeZone = marketHoursDbEntry.ExchangeHours.TimeZone;
            var symbolObject = QuantConnect.Symbol.Create(symbol, securityType, market);
            var config = new SubscriptionDataConfig(typeof(T), symbolObject, resolution, dataTimeZone, exchangeTimeZone, false, false, true, true);
            AddUniverse(new FuncUniverse(config, subscriptionSettings, d => selector(d.OfType<T>())));
        }

        /// <summary>
        /// Sets the current universe selector for the algorithm. This will be executed on day changes
        /// </summary>
        /// <param name="selector">Defines an initial coarse selection</param>
        public void AddUniverse(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> selector)
        {
            var symbol = CoarseFundamental.CreateUniverseSymbol(Market.USA);
            var config = new SubscriptionDataConfig(typeof(CoarseFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, true);
            AddUniverse(new FuncUniverse(config, UniverseSettings, selectionData => selector(selectionData.OfType<CoarseFundamental>())));
        }

        /// <summary>
        /// Adds the security to the user defined universe for the specified 
        /// </summary>
        private void AddToUserDefinedUniverse(Security security)
        {
            // add this security to the user defined universe
            UserDefinedUniverse universe;
            var key = new SecurityTypeMarket(security.Type, security.SubscriptionDataConfig.Market);
            if (_userDefinedUniverses.TryGetValue(key, out universe))
            {
                universe.Add(security.Symbol);
            }
            else
            {
                // create a new universe, these subscription settings don't currently get used
                // since universe selection proper is never invoked on this type of universe
                var securityConfig = security.SubscriptionDataConfig;
                var universeSymbol = UserDefinedUniverse.CreateSymbol(securityConfig.SecurityType, securityConfig.Market);
                var uconfig = new SubscriptionDataConfig(securityConfig, symbol: universeSymbol, isInternalFeed: true, fillForward: false);
                universe = new UserDefinedUniverse(uconfig,
                    new SubscriptionSettings(security.Resolution, security.Leverage, security.IsFillDataForward, security.IsExtendedMarketHours),
                    QuantConnect.Time.MaxTimeSpan,
                    new List<Symbol> { security.Symbol }
                    );
                _userDefinedUniverses[key] = universe;

                Universes.Add(universe);
            }
        }
    }
}
