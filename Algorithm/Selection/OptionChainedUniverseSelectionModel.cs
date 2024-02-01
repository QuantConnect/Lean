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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Securities.Future;
using Python.Runtime;

namespace QuantConnect.Algorithm.Selection
{
    /// <summary>
    /// This universe selection model will chain to the security changes of a given <see cref="Universe"/> selection
    /// output and create a new <see cref="OptionChainUniverse"/> for each of them
    /// </summary>
    public class OptionChainedUniverseSelectionModel : UniverseSelectionModel
    {
        private DateTime _nextRefreshTimeUtc;
        private IEnumerable<Symbol> _currentSymbols;
        private readonly UniverseSettings _universeSettings;
        private readonly Func<OptionFilterUniverse, OptionFilterUniverse> _optionFilter;

        /// <summary>
        /// Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.
        /// </summary>
        public override DateTime GetNextRefreshTimeUtc() => _nextRefreshTimeUtc;

        /// <summary>
        /// Creates a new instance of <see cref="OptionChainedUniverseSelectionModel"/>
        /// </summary>
        /// <param name="universe">The universe we want to chain to</param>
        /// <param name="optionFilter">The option filter universe to use</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public OptionChainedUniverseSelectionModel(Universe universe,
            Func<OptionFilterUniverse, OptionFilterUniverse> optionFilter,
            UniverseSettings universeSettings = null)
        {
            _optionFilter = optionFilter;
            _universeSettings = universeSettings;
            _nextRefreshTimeUtc = DateTime.MaxValue;

            _currentSymbols = Enumerable.Empty<Symbol>();
            universe.SelectionChanged += (sender, args) =>
            {
                // the universe we were watching changed, this will trigger a call to CreateUniverses
                _nextRefreshTimeUtc = DateTime.MinValue;

                // We must create the new option Symbol using the CreateOption(Symbol, ...) overload.
                // Otherwise, we'll end up loading equity data for the selected Symbol, which won't
                // work whenever we're loading options data for any non-equity underlying asset class.
                _currentSymbols = ((Universe.SelectionEventArgs)args).CurrentSelection
                    .Select(symbol => Symbol.CreateOption(
                        symbol,
                        symbol.ID.Market,
                        symbol.SecurityType.DefaultOptionStyle(),
                        default(OptionRight),
                        0m,
                        SecurityIdentifier.DefaultDate))
                    .ToList();
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="OptionChainedUniverseSelectionModel"/>
        /// </summary>
        /// <param name="universe">The universe we want to chain to</param>
        /// <param name="optionFilter">The python option filter universe to use</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public OptionChainedUniverseSelectionModel(Universe universe,
            PyObject optionFilter,
            UniverseSettings universeSettings = null): this(universe, ConvertOptionFilter(optionFilter), universeSettings)
        {
        }

        /// <summary>
        /// Creates the universes for this algorithm. Called once after <see cref="IAlgorithm.Initialize"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universes to be used by the algorithm</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            _nextRefreshTimeUtc = DateTime.MaxValue;

            foreach (var optionSymbol in _currentSymbols)
            {
                yield return algorithm.CreateOptionChain(optionSymbol, _optionFilter, _universeSettings);
            }
        }

        private static Func<OptionFilterUniverse, OptionFilterUniverse> ConvertOptionFilter(PyObject optionFilter)
        {
            using (Py.GIL())
            {
                return optionFilter.ConvertToDelegate<Func<OptionFilterUniverse, OptionFilterUniverse>>();
            }
        }
    }
}
