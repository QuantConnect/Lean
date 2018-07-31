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
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.Framework.Selection
{
    public class OptionUniverseSelectionModel : UniverseSelectionModel
    {
        private DateTime _nextRefreshTimeUtc;

        private readonly TimeSpan _refreshInterval;
        private readonly UniverseSettings _universeSettings;
        private readonly ISecurityInitializer _securityInitializer;
        private readonly Func<DateTime, IEnumerable<Symbol>> _optionChainSymbolSelector;

        public override DateTime GetNextRefreshTimeUtc() => _nextRefreshTimeUtc;

        public OptionUniverseSelectionModel(TimeSpan refreshInterval, Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
            : this(refreshInterval, optionChainSymbolSelector, null, null)
        {
        }

        public OptionUniverseSelectionModel(TimeSpan refreshInterval,
            Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector,
            UniverseSettings universeSettings,
            ISecurityInitializer securityInitializer
        )
        {
            _nextRefreshTimeUtc = DateTime.MinValue;

            _refreshInterval = refreshInterval;
            _universeSettings = universeSettings;
            _securityInitializer = securityInitializer;
            _optionChainSymbolSelector = optionChainSymbolSelector;
        }

        public override IEnumerable<Universe> CreateUniverses(QCAlgorithmFramework algorithm)
        {
            _nextRefreshTimeUtc = algorithm.UtcTime + _refreshInterval;

            var uniqueUnderlyingSymbols = new HashSet<Symbol>();
            foreach (var optionSymbol in _optionChainSymbolSelector(algorithm.UtcTime))
            {
                if (optionSymbol.SecurityType != SecurityType.Option)
                {
                    throw new ArgumentException("optionChainSymbolSelector must return option symbols.");
                }

                // prevent creating duplicate option chains -- one per underlying
                if (uniqueUnderlyingSymbols.Add(optionSymbol.Underlying))
                {
                    yield return CreateOptionChain(algorithm, optionSymbol);
                }
            }
        }

        protected virtual Option CreateOptionChainSecurity(QCAlgorithmFramework algorithm, Symbol symbol, UniverseSettings settings, ISecurityInitializer initializer)
        {
            var market = symbol.ID.Market;
            var underlying = symbol.Underlying;

            var marketHoursEntry = MarketHoursDatabase.FromDataFolder()
                .GetEntry(market, underlying, SecurityType.Option);

            var symbolProperties = SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolProperties(market, underlying, SecurityType.Option, CashBook.AccountCurrency);

            var optionChain = (Option)SecurityManager.CreateSecurity(typeof(ZipEntryName), algorithm.Portfolio,
                algorithm.SubscriptionManager, marketHoursEntry.ExchangeHours, marketHoursEntry.DataTimeZone, symbolProperties,
                initializer, symbol, settings.Resolution, settings.FillForward, settings.Leverage, settings.ExtendedMarketHours,
                false, false, algorithm.LiveMode, false, false);

            return optionChain;
        }

        /// <summary>
        /// Defines the option chain universe filter
        /// </summary>
        protected virtual OptionFilterUniverse Filter(OptionFilterUniverse filter)
        {
            // NOP
            return filter;
        }

        private OptionChainUniverse CreateOptionChain(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            if (symbol.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("CreateOptionChain requires an option symbol.");
            }

            // rewrite non-canonical symbols to be canonical
            var market = symbol.ID.Market;
            var underlying = symbol.Underlying;
            if (!symbol.IsCanonical())
            {
                var alias = $"?{underlying.Value}";
                symbol = Symbol.Create(underlying.Value, SecurityType.Option, market, alias);
            }

            // resolve defaults if not specified
            var settings = _universeSettings ?? algorithm.UniverseSettings;
            var initializer = _securityInitializer ?? algorithm.SecurityInitializer;

            // create canonical security object, but don't duplicate if it already exists
            Security security;
            Option optionChain;
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                optionChain = CreateOptionChainSecurity(algorithm, symbol, settings, initializer);
            }
            else
            {
                optionChain = (Option)security;
            }

            // set the option chain contract filter function
            optionChain.SetFilter(Filter);

            // force option chain security to not be directly tradable AFTER it's configured to ensure it's not overwritten
            optionChain.IsTradable = false;

            return new OptionChainUniverse(optionChain, settings, initializer, algorithm.LiveMode);
        }
    }
}