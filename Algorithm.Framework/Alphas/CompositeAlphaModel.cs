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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides an implementation of <see cref="IAlphaModel"/> that combines multiple alpha
    /// models into a single alpha model and properly sets each insights 'SourceModel' property.
    /// </summary>
    public class CompositeAlphaModel : IAlphaModel
    {
        private readonly IAlphaModel[] _alphaModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAlphaModel"/> class
        /// </summary>
        /// <param name="alphaModels">The individual alpha models defining this composite model</param>
        public CompositeAlphaModel(params IAlphaModel[] alphaModels)
        {
            if (alphaModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 alpha model for the CompositeAlphaModel");
            }

            _alphaModels = alphaModels;
        }

        public CompositeAlphaModel(PyObject[] alphaModels)
        {
            if (alphaModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 alpha model for the CompositeAlphaModel");
            }

            _alphaModels = new IAlphaModel[alphaModels.Length];

            for (var i = 0; i < alphaModels.Length; i++)
            {
                if (!alphaModels[i].TryConvert(out _alphaModels[i]))
                {
                    _alphaModels[i] = new AlphaModelPythonWrapper(alphaModels[i]);
                }
            }
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities.
        /// This method patches this call through the each of the wrapped models.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public virtual IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            foreach (var model in _alphaModels)
            {
                var name = model.GetModelName();
                foreach (var insight in model.Update(algorithm, data))
                {
                    if (string.IsNullOrEmpty(insight.SourceModel))
                    {
                        // set the source model name if not already set
                        insight.SourceModel = name;
                    }

                    yield return insight;
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed.
        /// This method patches this call through the each of the wrapped models.
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public virtual void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            foreach (var model in _alphaModels)
            {
                model.OnSecuritiesChanged(algorithm, changes);
            }
        }
    }
}
