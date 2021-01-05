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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Portfolio selection model that uses coarse/fine selectors. For US equities only.
    /// </summary>
    public class FineFundamentalUniverseSelectionModel : FundamentalUniverseSelectionModel
    {
        private readonly Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> _coarseSelector;
        private readonly Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> _fineSelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="FineFundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <param name="fineSelector">Selects symbols from the provided fine data set (this set has already been filtered according to the coarse selection)</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="securityInitializer">Performs extra initialization (such as setting models) after we create a new security object</param>
        public FineFundamentalUniverseSelectionModel(
            Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector,
            Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> fineSelector,
            UniverseSettings universeSettings = null,
            ISecurityInitializer securityInitializer = null
            )
            : base(true, universeSettings, securityInitializer)
        {
            _coarseSelector = coarseSelector;
            _fineSelector = fineSelector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FineFundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <param name="fineSelector">Selects symbols from the provided fine data set (this set has already been filtered according to the coarse selection)</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        /// <param name="securityInitializer">Performs extra initialization (such as setting models) after we create a new security object</param>
        public FineFundamentalUniverseSelectionModel(
            PyObject coarseSelector,
            PyObject fineSelector,
            UniverseSettings universeSettings = null,
            ISecurityInitializer securityInitializer = null
            )
            : base(true, universeSettings, securityInitializer)
        {
            Func<IEnumerable<FineFundamental>, object> fineFunc;
            Func<IEnumerable<CoarseFundamental>, object> coarseFunc;
            if (fineSelector.TryConvertToDelegate(out fineFunc) &&
                coarseSelector.TryConvertToDelegate(out coarseFunc))
            {
                _fineSelector = fineFunc.ConvertToUniverseSelectionSymbolDelegate();
                _coarseSelector = coarseFunc.ConvertToUniverseSelectionSymbolDelegate();
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
        {
            return _coarseSelector(coarse);
        }

        /// <inheritdoc />
        public override IEnumerable<Symbol> SelectFine(QCAlgorithm algorithm, IEnumerable<FineFundamental> fine)
        {
            return _fineSelector(fine);
        }
    }
}