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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that subscribes to option chains
    /// </summary>
    public class OptionUniverseSelectionModel : UniverseSelectionModel
    {
        private DateTime _nextRefreshTimeUtc;

        private readonly TimeSpan _refreshInterval;
        private readonly UniverseSettings _universeSettings;
        private readonly Func<DateTime, IEnumerable<Symbol>> _optionChainSymbolSelector;

        /// <summary>
        /// Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.
        /// </summary>
        public override DateTime GetNextRefreshTimeUtc() => _nextRefreshTimeUtc;

        /// <summary>
        /// Creates a new instance of <see cref="OptionUniverseSelectionModel"/>
        /// </summary>
        /// <param name="refreshInterval">Time interval between universe refreshes</param>
        /// <param name="optionChainSymbolSelector">Selects symbols from the provided option chain</param>
        public OptionUniverseSelectionModel(TimeSpan refreshInterval, Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
            : this(refreshInterval, optionChainSymbolSelector, null)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="OptionUniverseSelectionModel"/>
        /// </summary>
        /// <param name="refreshInterval">Time interval between universe refreshes</param>
        /// <param name="optionChainSymbolSelector">Selects symbols from the provided option chain</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="securityInitializer">Performs extra initialization (such as setting models) after we create a new security object</param>
        [Obsolete("This constructor is obsolete because SecurityInitializer is obsolete and will not be used.")]
        public OptionUniverseSelectionModel(
            TimeSpan refreshInterval,
            Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector,
            UniverseSettings universeSettings,
            ISecurityInitializer securityInitializer
            )
            :this (refreshInterval,optionChainSymbolSelector,universeSettings)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="OptionUniverseSelectionModel"/>
        /// </summary>
        /// <param name="refreshInterval">Time interval between universe refreshes</param>
        /// <param name="optionChainSymbolSelector">Selects symbols from the provided option chain</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public OptionUniverseSelectionModel(
            TimeSpan refreshInterval,
            Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector,
            UniverseSettings universeSettings
            )
        {
            _nextRefreshTimeUtc = DateTime.MinValue;

            _refreshInterval = refreshInterval;
            _universeSettings = universeSettings;
            _optionChainSymbolSelector = optionChainSymbolSelector;
        }

        /// <summary>
        /// Creates the universes for this algorithm. Called once after <see cref="IAlgorithm.Initialize"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universes to be used by the algorithm</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
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

        /// <summary>
        /// Creates the canonical <see cref="Option"/> chain security for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the option</param>
        /// <param name="settings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="initializer">Performs extra initialization (such as setting models) after we create a new security object</param>
        /// <returns><see cref="Option"/> for the given symbol</returns>
        [Obsolete("This method is obsolete because SecurityInitializer is obsolete and will not be used.")]
        protected virtual Option CreateOptionChainSecurity(QCAlgorithm algorithm, Symbol symbol, UniverseSettings settings, ISecurityInitializer initializer)
        {
            return CreateOptionChainSecurity(
                algorithm.SubscriptionManager.SubscriptionDataConfigService,
                symbol,
                settings,
                algorithm.Securities);
        }

        /// <summary>
        /// Creates the canonical <see cref="Option"/> chain security for a given symbol
        /// </summary>
        /// <param name="subscriptionDataConfigService">The service used to create new <see cref="SubscriptionDataConfig"/></param>
        /// <param name="symbol">Symbol of the option</param>
        /// <param name="settings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="securityManager">Used to create new <see cref="Security"/></param>
        /// <returns><see cref="Option"/> for the given symbol</returns>
        protected virtual Option CreateOptionChainSecurity(
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
                false);
            return (Option)securityManager.CreateSecurity(symbol, config, settings.Leverage, false);
        }

        /// <summary>
        /// Defines the option chain universe filter
        /// </summary>
        protected virtual OptionFilterUniverse Filter(OptionFilterUniverse filter)
        {
            // NOP
            return filter;
        }

        /// <summary>
        /// Creates a <see cref="OptionChainUniverse"/> for a given symbol
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <param name="symbol">Symbol of the option</param>
        /// <returns><see cref="OptionChainUniverse"/> for the given symbol</returns>
        private OptionChainUniverse CreateOptionChain(QCAlgorithm algorithm, Symbol symbol)
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

            // create canonical security object, but don't duplicate if it already exists
            Security security;
            Option optionChain;
            if (!algorithm.Securities.TryGetValue(symbol, out security))
            {
                optionChain = CreateOptionChainSecurity(
                    algorithm.SubscriptionManager.SubscriptionDataConfigService,
                    symbol,
                    settings,
                    algorithm.Securities);
            }
            else
            {
                optionChain = (Option)security;
            }

            // set the option chain contract filter function
            optionChain.SetFilter(Filter);

            // force option chain security to not be directly tradable AFTER it's configured to ensure it's not overwritten
            optionChain.IsTradable = false;

            return new OptionChainUniverse(optionChain, settings, algorithm.LiveMode);
        }
    }
}