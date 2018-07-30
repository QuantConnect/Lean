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
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.Framework.Selection
{
    public class FutureUniverseSelectionModel : UniverseSelectionModel
    {
        private DateTime _nextRefreshTimeUtc;

        private readonly TimeSpan _refreshInterval;
        private readonly UniverseSettings _universeSettings;
        private readonly ISecurityInitializer _securityInitializer;
        private readonly Func<DateTime, IEnumerable<Symbol>> _futureChainSymbolSelector;

        public override DateTime GetNextRefreshTimeUtc() => _nextRefreshTimeUtc;

        public FutureUniverseSelectionModel(TimeSpan refreshInterval, Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector)
            : this(refreshInterval, futureChainSymbolSelector, null, null)
        {
        }

        public FutureUniverseSelectionModel(TimeSpan refreshInterval,
            Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector,
            UniverseSettings universeSettings,
            ISecurityInitializer securityInitializer
        )
        {
            _nextRefreshTimeUtc = DateTime.MinValue;

            _refreshInterval = refreshInterval;
            _universeSettings = universeSettings;
            _securityInitializer = securityInitializer;
            _futureChainSymbolSelector = futureChainSymbolSelector;
        }

        public override IEnumerable<Universe> CreateUniverses(QCAlgorithmFramework algorithm)
        {
            _nextRefreshTimeUtc = algorithm.UtcTime + _refreshInterval;

            algorithm.Log($"FutureUniverseSelectionModel.CreateUniverse({algorithm.UtcTime}): Refreshing Universes");

            var uniqueSymbols = new HashSet<Symbol>();
            foreach (var futureSymbol in _futureChainSymbolSelector(algorithm.UtcTime))
            {
                if (futureSymbol.SecurityType != SecurityType.Future)
                {
                    throw new ArgumentException("FutureChainSymbolSelector must return future symbols.");
                }

                // prevent creating duplicate future chains -- one per symbol
                if (uniqueSymbols.Add(futureSymbol))
                {
                    yield return CreateFutureChain(algorithm, futureSymbol);
                }
            }
        }

        protected virtual Future CreateFutureChainSecurity(QCAlgorithmFramework algorithm, Symbol symbol, UniverseSettings settings, ISecurityInitializer initializer)
        {
            algorithm.Log($"FutureUniverseSelectionModel.CreateFutureChainSecurity({algorithm.UtcTime}, {symbol}): Creating Future Chain Security");

            var market = symbol.ID.Market;

            var marketHoursEntry = MarketHoursDatabase.FromDataFolder()
                .GetEntry(market, symbol, SecurityType.Future);

            var symbolProperties = SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolProperties(market, symbol, SecurityType.Future, CashBook.AccountCurrency);

            return (Future)SecurityManager.CreateSecurity(typeof(ZipEntryName), algorithm.Portfolio,
                algorithm.SubscriptionManager, marketHoursEntry.ExchangeHours, marketHoursEntry.DataTimeZone, symbolProperties,
                initializer, symbol, settings.Resolution, settings.FillForward, settings.Leverage, settings.ExtendedMarketHours,
                false, false, algorithm.LiveMode, false, false);
        }

        /// <summary>
        /// Defines the future chain universe filter
        /// </summary>
        protected virtual FutureFilterUniverse Filter(FutureFilterUniverse filter)
        {
            // NOP
            return filter;
        }

        private FuturesChainUniverse CreateFutureChain(QCAlgorithmFramework algorithm, Symbol symbol)
        {
            if (symbol.SecurityType != SecurityType.Future)
            {
                throw new ArgumentException("CreateFutureChain requires a future symbol.");
            }

            algorithm.Log($"FutureUniverseSelectionModel.CreateFutureChain({algorithm.UtcTime}, {symbol}): Creating Future Chain");

            // rewrite non-canonical symbols to be canonical
            var market = symbol.ID.Market;
            if (!symbol.IsCanonical())
            {
                symbol = Symbol.Create(symbol.Value, SecurityType.Future, market, $"/{symbol.Value}");
            }

            // resolve defaults if not specified
            var settings = _universeSettings ?? algorithm.UniverseSettings;
            var initializer = _securityInitializer ?? algorithm.SecurityInitializer;

            // create canonical security object, but don't duplicate if it already exists
            Security security;
            Future futureChain;
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                futureChain = CreateFutureChainSecurity(algorithm, symbol, settings, initializer);
            }
            else
            {
                futureChain = (Future)security;

                algorithm.Log($"FutureUniverseSelectionModel.CreateFutureChain({algorithm.UtcTime}, {symbol}): Resolved existing Future Chain Security");
            }

            // set the future chain contract filter function
            futureChain.SetFilter(Filter);

            // force future chain security to not be directly tradable AFTER it's configured to ensure it's not overwritten
            futureChain.IsTradable = false;

            return new FuturesChainUniverse(futureChain, settings, algorithm.SubscriptionManager, initializer);
        }
    }
}