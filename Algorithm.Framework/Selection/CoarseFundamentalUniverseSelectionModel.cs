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
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Portfolio selection model that uses coarse selectors. For US equities only.
    /// </summary>
    public class CoarseFundamentalUniverseSelectionModel : FundamentalUniverseSelectionModel
    {
        private readonly Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> _coarseSelector;
        /// <summary>
        /// Initializes a new instance of the <see cref="CoarseFundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public CoarseFundamentalUniverseSelectionModel(
            Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector,
            UniverseSettings universeSettings = null
            )
            : base(false, universeSettings)
        {
            _coarseSelector = coarseSelector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoarseFundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public CoarseFundamentalUniverseSelectionModel(
            PyObject coarseSelector,
            UniverseSettings universeSettings = null
            )
            : base(false, universeSettings)
        {
            if (coarseSelector.TrySafeAs<Func<IEnumerable<CoarseFundamental>, object>>(out var func))
            {
                _coarseSelector = func.ConvertToUniverseSelectionSymbolDelegate();
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
        {
            return _coarseSelector(coarse);
        }
    }
}
