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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

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
                if (optionSymbol.SecurityType != SecurityType.Option && optionSymbol.SecurityType != SecurityType.FutureOption)
                {
                    throw new ArgumentException("optionChainSymbolSelector must return option or futures options symbols.");
                }

                // prevent creating duplicate option chains -- one per underlying
                if (uniqueUnderlyingSymbols.Add(optionSymbol.Underlying))
                {
                    yield return algorithm.CreateOptionChain(optionSymbol, Filter, _universeSettings);
                }
            }
        }

        /// <summary>
        /// Defines the option chain universe filter
        /// </summary>
        protected virtual OptionFilterUniverse Filter(OptionFilterUniverse filter)
        {
            // NOP
            return filter;
        }
    }
}
