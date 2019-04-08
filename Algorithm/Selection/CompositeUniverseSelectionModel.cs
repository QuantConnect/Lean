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
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that combines multiple universe
    /// selection models into a single model.
    /// </summary>
    public class CompositeUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly List<IUniverseSelectionModel> _universeSelectionModels = new List<IUniverseSelectionModel>();
        private bool _alreadyCalledCreateUniverses;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="universeSelectionModels">The individual universe selection models defining this composite model</param>
        public CompositeUniverseSelectionModel(params IUniverseSelectionModel[] universeSelectionModels)
        {
            if (universeSelectionModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 universe selection model for the CompositeUniverseSelectionModel");
            }

            _universeSelectionModels.AddRange(universeSelectionModels);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="universeSelectionModels">The individual universe selection models defining this composite model</param>
        public CompositeUniverseSelectionModel(params PyObject[] universeSelectionModels)
        {
            if (universeSelectionModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 universe selection model for the CompositeUniverseSelectionModel");
            }

            foreach (var pyUniverseSelectionModel in universeSelectionModels)
            {
                AddUniverseSelection(pyUniverseSelectionModel);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="universeSelectionModel">The individual universe selection model defining this composite model</param>
        public CompositeUniverseSelectionModel(PyObject universeSelectionModel)
            : this(new[] { universeSelectionModel })
        {

        }

        /// <summary>
        /// Adds a new <see cref="IUniverseSelectionModel"/>
        /// </summary>
        /// <param name="universeSelectionModel">The universe selection model to add</param>
        public void AddUniverseSelection(IUniverseSelectionModel universeSelectionModel)
        {
            _universeSelectionModels.Add(universeSelectionModel);
        }

        /// <summary>
        /// Adds a new <see cref="IUniverseSelectionModel"/>
        /// </summary>
        /// <param name="pyUniverseSelectionModel">The universe selection model to add</param>
        public void AddUniverseSelection(PyObject pyUniverseSelectionModel)
        {
            IUniverseSelectionModel selectionModel;
            if (!pyUniverseSelectionModel.TryConvert(out selectionModel))
            {
                selectionModel = new UniverseSelectionModelPythonWrapper(pyUniverseSelectionModel);
            }
            _universeSelectionModels.Add(selectionModel);
        }

        /// <summary>
        /// Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.
        /// </summary>
        public override DateTime GetNextRefreshTimeUtc()
        {
            return _universeSelectionModels.Min(model => model.GetNextRefreshTimeUtc());
        }

        /// <summary>
        /// Creates the universes for this algorithm.
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universes to be used by the algorithm</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            foreach (var universeSelectionModel in _universeSelectionModels)
            {
                var selectionRefreshTime = universeSelectionModel.GetNextRefreshTimeUtc();
                var refreshTime = algorithm.UtcTime >= selectionRefreshTime;
                if (!_alreadyCalledCreateUniverses // first initial call
                    || refreshTime
                    || selectionRefreshTime == DateTime.MaxValue)
                {
                    foreach (var universe in universeSelectionModel.CreateUniverses(algorithm))
                    {
                        yield return universe;
                    }
                }
            }
            _alreadyCalledCreateUniverses = true;
        }
    }
}
