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
using Python.Runtime;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides a universe that can be filtered with a <see cref="Fundamental.Fundamental"/> selection function
    /// </summary>
    public class FundamentalFilteredUniverse : SelectSymbolsUniverseDecorator
    {
        /// <summary>
        /// The universe that will be used for fine universe selection
        /// </summary>
        public FundamentalUniverseFactory FundamentalUniverse { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalFilteredUniverse"/> class
        /// </summary>
        /// <param name="universe">The universe to be filtered</param>
        /// <param name="fundamentalSelector">The fundamental selection function</param>
        public FundamentalFilteredUniverse(Universe universe, Func<IEnumerable<Fundamental.Fundamental>, IEnumerable<Symbol>> fundamentalSelector)
            : base(universe, universe.SelectSymbols)
        {
            FundamentalUniverse = Fundamental.FundamentalUniverse.USA(fundamentalSelector, universe.UniverseSettings);
            FundamentalUniverse.SelectionChanged += (sender, args) => OnSelectionChanged(((SelectionEventArgs)args).CurrentSelection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalFilteredUniverse"/> class
        /// </summary>
        /// <param name="universe">The universe to be filtered</param>
        /// <param name="fundamentalSelector">The fundamental selection function</param>
        public FundamentalFilteredUniverse(Universe universe, PyObject fundamentalSelector)
            : base(universe, universe.SelectSymbols)
        {
            var func = fundamentalSelector.ConvertToDelegate<Func<IEnumerable<Fundamental.Fundamental>, object>>();
            FundamentalUniverse = Fundamental.FundamentalUniverse.USA(func.ConvertToUniverseSelectionSymbolDelegate(), universe.UniverseSettings);
            FundamentalUniverse.SelectionChanged += (sender, args) => OnSelectionChanged(((SelectionEventArgs)args).CurrentSelection);
        }
    }
}
