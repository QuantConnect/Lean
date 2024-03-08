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
using Python.Runtime;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides a base class for defining equity coarse/fine fundamental selection models
    /// </summary>
    public class FundamentalUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly string _market;
        private readonly bool _fundamentalData;
        private readonly bool _filterFineData;
        private readonly UniverseSettings _universeSettings;
        private readonly Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> _selector;

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        public FundamentalUniverseSelectionModel()
            : this(Market.USA, null)
        {
            _fundamentalData = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="market">The target market</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FundamentalUniverseSelectionModel(string market, UniverseSettings universeSettings)
        {
            _market = market;
            _fundamentalData = true;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FundamentalUniverseSelectionModel(UniverseSettings universeSettings)
           : this(Market.USA, universeSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="market">The target market</param>
        /// <param name="selector">Selects symbols from the provided fundamental data set</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FundamentalUniverseSelectionModel(string market, Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> selector, UniverseSettings universeSettings = null)
        {
            _market = market;
            _selector = selector;
            _fundamentalData = true;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="selector">Selects symbols from the provided fundamental data set</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FundamentalUniverseSelectionModel(Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> selector, UniverseSettings universeSettings = null)
           : this(Market.USA, selector, universeSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="market">The target market</param>
        /// <param name="selector">Selects symbols from the provided fundamental data set</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FundamentalUniverseSelectionModel(string market, PyObject selector, UniverseSettings universeSettings = null) : this(universeSettings)
        {
            _market = market;
            Func<IEnumerable<Fundamental>, object> selectorFunc;
            if (selector.TryConvertToDelegate(out selectorFunc))
            {
                _selector = selectorFunc.ConvertToUniverseSelectionSymbolDelegate();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="selector">Selects symbols from the provided fundamental data set</param>
        /// <param name="universeSettings">Universe settings define attributes of created subscriptions, such as their resolution and the minimum time in universe before they can be removed</param>
        public FundamentalUniverseSelectionModel(PyObject selector, UniverseSettings universeSettings = null)
           : this(Market.USA, selector, universeSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="filterFineData">True to also filter using fine fundamental data, false to only filter on coarse data</param>
        [Obsolete("Fine and Coarse selection are merged, please use 'FundamentalUniverseSelectionModel()'")]
        protected FundamentalUniverseSelectionModel(bool filterFineData)
            : this(filterFineData, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FundamentalUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="filterFineData">True to also filter using fine fundamental data, false to only filter on coarse data</param>
        /// <param name="universeSettings">The settings used when adding symbols to the algorithm, specify null to use algorithm.UniverseSettings</param>
        [Obsolete("Fine and Coarse selection are merged, please use 'FundamentalUniverseSelectionModel(UniverseSettings)'")]
        protected FundamentalUniverseSelectionModel(bool filterFineData, UniverseSettings universeSettings)
        {
            _market = Market.USA;
            _filterFineData = filterFineData;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Creates a new fundamental universe using this class's selection functions
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universe defined by this model</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            if (_fundamentalData)
            {
                var universeSettings = _universeSettings ?? algorithm.UniverseSettings;
                yield return new FundamentalUniverseFactory(_market, universeSettings, fundamental => Select(algorithm, fundamental));
            }
            else
            {
                // for backwards compatibility
                var universe = CreateCoarseFundamentalUniverse(algorithm);
                if (_filterFineData)
                {
                    if (universe.UniverseSettings.Asynchronous.HasValue && universe.UniverseSettings.Asynchronous.Value)
                    {
                        throw new ArgumentException("Asynchronous universe setting is not supported for coarse & fine selections, please use the new Fundamental single pass selection");
                    }
#pragma warning disable CS0618 // Type or member is obsolete
                    universe = new FineFundamentalFilteredUniverse(universe, fine => SelectFine(algorithm, fine));
#pragma warning restore CS0618 // Type or member is obsolete
                }
                yield return universe;
            }
        }

        /// <summary>
        /// Creates the coarse fundamental universe object.
        /// This is provided to allow more flexibility when creating coarse universe.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>The coarse fundamental universe</returns>
        public virtual Universe CreateCoarseFundamentalUniverse(QCAlgorithm algorithm)
        {
            var universeSettings = _universeSettings ?? algorithm.UniverseSettings;
            return new CoarseFundamentalUniverse(universeSettings, coarse =>
            {
                // if we're using fine fundamental selection than exclude symbols without fine data
                if (_filterFineData)
                {
                    coarse = coarse.Where(c => c.HasFundamentalData);
                }

#pragma warning disable CS0618 // Type or member is obsolete
                return SelectCoarse(algorithm, coarse);
#pragma warning restore CS0618 // Type or member is obsolete
            });
        }

        /// <summary>
        /// Defines the fundamental selection function.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="fundamental">The fundamental data used to perform filtering</param>
        /// <returns>An enumerable of symbols passing the filter</returns>
        public virtual IEnumerable<Symbol> Select(QCAlgorithm algorithm, IEnumerable<Fundamental> fundamental)
        {
            if(_selector == null)
            {
                throw new NotImplementedException("If inheriting, please overrride the 'Select' fundamental function, else provide it as a constructor parameter");
            }
            return _selector(fundamental);
        }

        /// <summary>
        /// Defines the coarse fundamental selection function.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="coarse">The coarse fundamental data used to perform filtering</param>
        /// <returns>An enumerable of symbols passing the filter</returns>
        [Obsolete("Fine and Coarse selection are merged, please use 'Select(QCAlgorithm, IEnumerable<Fundamental>)'")]
        public virtual IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm, IEnumerable<CoarseFundamental> coarse)
        {
            throw new NotImplementedException("Please overrride the 'Select' fundamental function");
        }

        /// <summary>
        /// Defines the fine fundamental selection function.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="fine">The fine fundamental data used to perform filtering</param>
        /// <returns>An enumerable of symbols passing the filter</returns>
        [Obsolete("Fine and Coarse selection are merged, please use 'Select(QCAlgorithm, IEnumerable<Fundamental>)'")]
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
        [Obsolete("Fine and Coarse selection are merged, please use 'Fundamental(Func<IEnumerable<Fundamental>, IEnumerable<Symbol>>)'")]
        public static IUniverseSelectionModel Coarse(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector)
        {
            return new CoarseFundamentalUniverseSelectionModel(coarseSelector);
        }

        /// <summary>
        /// Convenience method for creating a selection model that uses coarse and fine data
        /// </summary>
        /// <param name="coarseSelector">Selects symbols from the provided coarse data set</param>
        /// <param name="fineSelector">Selects symbols from the provided fine data set (this set has already been filtered according to the coarse selection)</param>
        /// <returns>A new universe selection model that will select US equities according to the selection functions specified</returns>
        [Obsolete("Fine and Coarse selection are merged, please use 'Fundamental(Func<IEnumerable<Fundamental>, IEnumerable<Symbol>>)'")]
        public static IUniverseSelectionModel Fine(Func<IEnumerable<CoarseFundamental>, IEnumerable<Symbol>> coarseSelector, Func<IEnumerable<FineFundamental>, IEnumerable<Symbol>> fineSelector)
        {
            return new FineFundamentalUniverseSelectionModel(coarseSelector, fineSelector);
        }

        /// <summary>
        /// Convenience method for creating a selection model that uses fundamental data
        /// </summary>
        /// <param name="selector">Selects symbols from the provided fundamental data set</param>
        /// <returns>A new universe selection model that will select US equities according to the selection functions specified</returns>
        public static IUniverseSelectionModel Fundamental(Func<IEnumerable<Fundamental>, IEnumerable<Symbol>> selector)
        {
            return new FundamentalUniverseSelectionModel(selector);
        }
    }
}
