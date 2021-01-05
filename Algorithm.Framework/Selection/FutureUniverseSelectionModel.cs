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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that subscribes to future chains
    /// </summary>
    public class FutureUniverseSelectionModel : UniverseSelectionModel
    {
        private DateTime _nextRefreshTimeUtc;

        private readonly TimeSpan _refreshInterval;
        private readonly UniverseSettings _universeSettings;
        private readonly Func<DateTime, IEnumerable<Symbol>> _futureChainSymbolSelector;

        /// <summary>
        /// Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.
        /// </summary>
        public override DateTime GetNextRefreshTimeUtc() => _nextRefreshTimeUtc;

        /// <summary>
        /// Creates a new instance of <see cref="FutureUniverseSelectionModel"/>
        /// </summary>
        /// <param name="refreshInterval">Time interval between universe refreshes</param>
        /// <param name="futureChainSymbolSelector">Selects symbols from the provided future chain</param>
        public FutureUniverseSelectionModel(TimeSpan refreshInterval, Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector)
            : this(refreshInterval, futureChainSymbolSelector, null)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="FutureUniverseSelectionModel"/>
        /// </summary>
        /// <param name="refreshInterval">Time interval between universe refreshes</param>
        /// <param name="futureChainSymbolSelector">Selects symbols from the provided future chain</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="securityInitializer">Performs extra initialization (such as setting models) after we create a new security object</param>
        [Obsolete("This constructor is obsolete because SecurityInitializer is obsolete and will not be used.")]
        public FutureUniverseSelectionModel(
            TimeSpan refreshInterval,
            Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector,
            UniverseSettings universeSettings,
            ISecurityInitializer securityInitializer
            )
            : this(refreshInterval, futureChainSymbolSelector, universeSettings)
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="FutureUniverseSelectionModel"/>
        /// </summary>
        /// <param name="refreshInterval">Time interval between universe refreshes</param>
        /// <param name="futureChainSymbolSelector">Selects symbols from the provided future chain</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FutureUniverseSelectionModel(
            TimeSpan refreshInterval,
            Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector,
            UniverseSettings universeSettings
            )
        {
            _nextRefreshTimeUtc = DateTime.MinValue;

            _refreshInterval = refreshInterval;
            _universeSettings = universeSettings;
            _futureChainSymbolSelector = futureChainSymbolSelector;
        }

        /// <summary>
        /// Creates the universes for this algorithm. Called once after <see cref="IAlgorithm.Initialize"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universes to be used by the algorithm</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            _nextRefreshTimeUtc = algorithm.UtcTime + _refreshInterval;

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

        /// <summary>
        /// Creates the canonical <see cref="Future"/> chain security for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the future</param>
        /// <param name="settings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="initializer">Performs extra initialization (such as setting models) after we create a new security object</param>
        /// <returns><see cref="Future"/> for the given symbol</returns>
        [Obsolete("This method is obsolete because SecurityInitializer is obsolete and will not be used.")]
        protected virtual Future CreateFutureChainSecurity(QCAlgorithm algorithm, Symbol symbol, UniverseSettings settings, ISecurityInitializer initializer)
        {
            return CreateFutureChainSecurity(
                algorithm.SubscriptionManager.SubscriptionDataConfigService,
                symbol,
                settings,
                algorithm.Securities);
        }

        /// <summary>
        /// Creates the canonical <see cref="Future"/> chain security for a given symbol
        /// </summary>
        /// <param name="subscriptionDataConfigService">The service used to create new <see cref="SubscriptionDataConfig"/></param>
        /// <param name="symbol">Symbol of the future</param>
        /// <param name="settings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="securityManager">Used to create new <see cref="Security"/></param>
        /// <returns><see cref="Future"/> for the given symbol</returns>
        protected virtual Future CreateFutureChainSecurity(
            ISubscriptionDataConfigService subscriptionDataConfigService,
            Symbol symbol,
            UniverseSettings settings,
            SecurityManager securityManager)
        {
            var config = subscriptionDataConfigService.Add(
                typeof(ZipEntryName),
                symbol,
                settings.Resolution,
                settings.FillForward,
                settings.ExtendedMarketHours,
                isFilteredSubscription: false);
            return (Future)securityManager.CreateSecurity(symbol, config, settings.Leverage, false);
        }

        /// <summary>
        /// Defines the future chain universe filter
        /// </summary>
        protected virtual FutureFilterUniverse Filter(FutureFilterUniverse filter)
        {
            // NOP
            return filter;
        }

        /// <summary>
        /// Creates a <see cref="FuturesChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the future</param>
        /// <returns><see cref="FuturesChainUniverse"/> for the given symbol</returns>
        private FuturesChainUniverse CreateFutureChain(QCAlgorithm algorithm, Symbol symbol)
        {
            if (symbol.SecurityType != SecurityType.Future)
            {
                throw new ArgumentException("CreateFutureChain requires a future symbol.");
            }

            // rewrite non-canonical symbols to be canonical
            var market = symbol.ID.Market;
            if (!symbol.IsCanonical())
            {
                symbol = Symbol.Create(symbol.Value, SecurityType.Future, market, $"/{symbol.Value}");
            }

            // resolve defaults if not specified
            var settings = _universeSettings ?? algorithm.UniverseSettings;

            // create canonical security object, but don't duplicate if it already exists
            Security security;
            Future futureChain;
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                futureChain = CreateFutureChainSecurity(
                    algorithm.SubscriptionManager.SubscriptionDataConfigService,
                    symbol,
                    settings,
                    algorithm.Securities);
            }
            else
            {
                futureChain = (Future)security;
            }

            // set the future chain contract filter function
            futureChain.SetFilter(Filter);

            // force future chain security to not be directly tradable AFTER it's configured to ensure it's not overwritten
            futureChain.IsTradable = false;

            return new FuturesChainUniverse(futureChain, settings);
        }
    }
}