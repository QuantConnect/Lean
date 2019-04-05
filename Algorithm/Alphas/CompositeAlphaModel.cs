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
    public class CompositeAlphaModel : AlphaModel
    {
        private readonly List<IAlphaModel> _alphaModels = new List<IAlphaModel>();

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

            _alphaModels.AddRange(alphaModels);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAlphaModel"/> class
        /// </summary>
        /// <param name="alphaModels">The individual alpha models defining this composite model</param>
        public CompositeAlphaModel(params PyObject[] alphaModels)
        {
            if (alphaModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 alpha model for the CompositeAlphaModel");
            }

            foreach (var pyAlphaModel in alphaModels)
            {
                AddAlpha(pyAlphaModel);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAlphaModel"/> class
        /// </summary>
        /// <param name="alphaModel">The individual alpha model defining this composite model</param>
        public CompositeAlphaModel(PyObject alphaModel)
            : this(new[] { alphaModel} )
        {

        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities.
        /// This method patches this call through the each of the wrapped models.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
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
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var model in _alphaModels)
            {
                model.OnSecuritiesChanged(algorithm, changes);
            }
        }

        /// <summary>
        /// Adds a new <see cref="AlphaModel"/>
        /// </summary>
        /// <param name="alphaModel">The alpha model to add</param>
        public void AddAlpha(IAlphaModel alphaModel)
        {
            _alphaModels.Add(alphaModel);
        }

        /// <summary>
        /// Adds a new <see cref="AlphaModel"/>
        /// </summary>
        /// <param name="pyAlphaModel">The alpha model to add</param>
        public void AddAlpha(PyObject pyAlphaModel)
        {
            IAlphaModel alphaModel;
            if (!pyAlphaModel.TryConvert(out alphaModel))
            {
                alphaModel = new AlphaModelPythonWrapper(pyAlphaModel);
            }
            _alphaModels.Add(alphaModel);
        }
    }
}
