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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

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
                    foreach (var universe in algorithm.CreateFutureChain(futureSymbol, Filter, _universeSettings))
                    {
                        yield return universe;
                    }
                }
            }
        }

        /// <summary>
        /// Defines the future chain universe filter
        /// </summary>
        protected virtual FutureFilterUniverse Filter(FutureFilterUniverse filter)
        {
            // NOP
            return filter;
        }
    }
}
