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
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that simply
    /// subscribes to the specified set of symbols
    /// </summary>
    public class ManualUniverseSelectionModel : IUniverseSelectionModel
    {
        private readonly IReadOnlyList<Symbol> _symbols;
        private readonly UniverseSettings _universeSettings;
        private readonly ISecurityInitializer _securityInitializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualUniverseSelectionModel"/> class using the algorithm's
        /// security initializer and universe settings
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to</param>
        public ManualUniverseSelectionModel(IEnumerable<Symbol> symbols)
            : this(symbols.ToArray())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualUniverseSelectionModel"/> class using the algorithm's
        /// security initializer and universe settings
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to</param>
        public ManualUniverseSelectionModel(params Symbol[] symbols)
            : this (symbols?.AsEnumerable(), null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to</param>
        /// <param name="universeSettings">The settings used when adding symbols to the algorithm, specify null to use algorthm.UniverseSettings</param>
        /// <param name="securityInitializer">Optional security initializer invoked when creating new securities, specify null to use algorithm.SecurityInitializer</param>
        public ManualUniverseSelectionModel(IEnumerable<Symbol> symbols, UniverseSettings universeSettings, ISecurityInitializer securityInitializer)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            _symbols = symbols.Where(s => !s.IsCanonical()).ToList();
            _universeSettings = universeSettings;
            _securityInitializer = securityInitializer;

            foreach (var symbol in _symbols)
            {
                SymbolCache.Set(symbol.Value, symbol);
            }
        }

        /// <summary>
        /// Creates the universes for this algorithm.
        /// Called at algorithm start.
        /// </summary>
        /// <returns>The universes defined by this model</returns>
        public IEnumerable<Universe> CreateUniverses(QCAlgorithmFramework algorithm)
        {
            var universeSettings = _universeSettings ?? algorithm.UniverseSettings;
            var securityInitializer = _securityInitializer ?? algorithm.SecurityInitializer;

            var resolution = universeSettings.Resolution;
            var type = resolution == Resolution.Tick ? typeof(Tick) : typeof(TradeBar);

            // universe per security type/market
            foreach (var grp in _symbols.GroupBy(s => new { s.ID.Market, s.SecurityType }))
            {
                var market = grp.Key.Market;
                var securityType = grp.Key.SecurityType;
                var universeSymbol = Symbol.Create($"manual-portfolio-selection-model-{securityType}-{market}", securityType, market);
                var entry = MarketHoursDatabase.FromDataFolder().GetEntry(market, (string)null, securityType);
                var config = new SubscriptionDataConfig(type, universeSymbol, resolution, entry.DataTimeZone, entry.ExchangeHours.TimeZone, false, false, true);
                yield return new ManualUniverse(config, universeSettings, securityInitializer, grp);
            }
        }
    }
}
