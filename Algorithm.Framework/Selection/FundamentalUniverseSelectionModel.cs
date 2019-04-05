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
using System.Linq;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides a base class for defining equity coarse/fine fundamental selection models
    /// </summary>
    public abstract class FundamentalUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly bool _filterFineData;
        private readonly UniverseSettings _universeSettings;
        private readonly ISecurityInitializer _securityInitializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="filterFineData">True to also filter using fine fundamental data, false to only filter on coarse data</param>
        protected FundamentalUniverseSelectionModel(bool filterFineData)
            : this(filterFineData, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="filterFineData">True to also filter using fine fundamental data, false to only filter on coarse data</param>
        /// <param name="universeSettings">The settings used when adding symbols to the algorithm, specify null to use algorthm.UniverseSettings</param>
        /// <param name="securityInitializer">Optional security initializer invoked when creating new securities, specify null to use algorithm.SecurityInitializer</param>
        protected FundamentalUniverseSelectionModel(bool filterFineData, UniverseSettings universeSettings, ISecurityInitializer securityInitializer)
        {
            _filterFineData = filterFineData;
            _universeSettings = universeSettings;
            _securityInitializer = securityInitializer;
        }

        /// <summary>
        /// Creates a new fundamental universe using this class's selection functions
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universe defined by this model</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            var universe = CreateCoarseFundamentalUniverse(algorithm);
            if (_filterFineData)
            {
                universe = new FineFundamentalFilteredUniverse(universe, fine => SelectFine(algorithm, fine));
            }
            yield return universe;
        }

        /// <summary>
        /// Creates the coarse fundamental universe object.
        /// This is provided to allow more flexibility when creating coarse universe, such as using algorithm.Universe.DollarVolume.Top(5)
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>The coarse fundamental universe</returns>
        public virtual Universe CreateCoarseFundamentalUniverse(QCAlgorithm algorithm)
        {
            var universeSettings = _universeSettings ?? algorithm.UniverseSettings;
            var securityInitializer = _securityInitializer ?? algorithm.SecurityInitializer;
            return new CoarseFundamentalUniverse(universeSettings, securityInitializer, coarse =>
            {
                // if we're using fine fundamental selection than exclude symbols without fine data
                if (_filterFineData)
                {
                    coarse = coarse.Where(c => c.HasFundamentalData);
                }

                return SelectCoarse(algorithm, coarse);
            });
        }

        /// <summary>
        /// Defines the coarse fundamental selection function.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="coarse">The coarse fundamental data used to perform filtering</param>
        /// <returns>An enumerable of symbols passing the filter</returns>
        public abstract IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse);

        /// <summary>
        /// Defines the fine fundamental selection function.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="fine">The fine fundamental data used to perform filtering</param>
        /// <returns>An enumerable of symbols passing the filter</returns>
        public virtual IEnumerable<Symbol> SelectFine(QCAlgorithm algorithm, IEnumerable<FineFundamental> fine)
        {
            // default impl performs no filtering of fine data
            return fine.Select(f => f.Symbol);
        }

        /// <summary>
        /// Convenience method for creating a selection model that uses only coarse data
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <returns>A new universe selection model that will select US equities according to the selection function specified</returns>
        public static IUniverseSelectionModel Coarse(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector)
        {
            return new CoarseFundamentalUniverseSelectionModel(coarseSelector);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <param name="fineSelector">Selects symbols from the provided fine data set (this set has already been filtered according to the coarse selection)</param>
        /// <returns>A new universe selection model that will select US equities according to the selection functions specified</returns>
        public static IUniverseSelectionModel Fine(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector, Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> fineSelector)
        {
            return new FineFundamentalUniverseSelectionModel(coarseSelector, fineSelector);
        }
    }
}
