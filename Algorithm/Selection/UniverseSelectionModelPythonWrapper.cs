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

using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class UniverseSelectionModelPythonWrapper : UniverseSelectionModel
    {
        private readonly dynamic _model;
        private readonly bool _modelHasGetNextRefreshTime;

        /// <summary>
        /// Gets the next time the framework should invoke the `CreateUniverses` method to refresh the set of universes.
        /// </summary>
        public override DateTime GetNextRefreshTimeUtc()
        {
            if (!_modelHasGetNextRefreshTime)
            {
                return DateTime.MaxValue;
            }

            using (Py.GIL())
            {
                return (_model.GetNextRefreshTimeUtc() as PyObject).GetAndDispose<DateTime>();
            }
        }

        /// <summary>
        /// Constructor for initialising the <see cref="IUniverseSelectionModel"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Model defining universes for the algorithm</param>
        public UniverseSelectionModelPythonWrapper(PyObject model)
        {
            using (Py.GIL())
            {
                _modelHasGetNextRefreshTime = model.HasAttr(nameof(IUniverseSelectionModel.GetNextRefreshTimeUtc));

                foreach (var attributeName in new[] { "CreateUniverses" })
                {
                    if (!model.HasAttr(attributeName))
                    {
                        throw new NotImplementedException($"IPortfolioSelectionModel.{attributeName} must be implemented. Please implement this missing method on {model.GetPythonType()}");
                    }
                }
            }
            _model = model;
        }

        /// <summary>
        /// Creates the universes for this algorithm. Called once after <see cref="IAlgorithm.Initialize"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universes to be used by the algorithm</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            using (Py.GIL())
            {
                var universes = _model.CreateUniverses(algorithm) as PyObject;
                var iterator = universes.GetIterator();
                foreach (PyObject universe in iterator)
                {
                    yield return universe.GetAndDispose<Universe>();
                }
                iterator.Dispose();
                universes.Dispose();
            }
        }
    }
}