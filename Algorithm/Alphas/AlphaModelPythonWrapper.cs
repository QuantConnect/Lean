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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Collections.Generic;
using QuantConnect.Python;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides an implementation of <see cref="IAlphaModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class AlphaModelPythonWrapper : AlphaModel
    {
        private readonly BasePythonWrapper<AlphaModel> _model;

        /// <summary>
        /// Defines a name for a framework model
        /// </summary>
        public override string Name
        {
            get
            {
                using (Py.GIL())
                {
                    // if the model defines a Name property then use that
                    if (_model.HasAttr(nameof(Name)))
                    {
                        return _model.GetProperty<string>(nameof(Name));
                    }

                    // if the model does not define a name property, use the python type name
                    return _model.GetProperty(" __class__" ).GetAttr("__name__").GetAndDispose<string>();
                }
            }
        }

        /// <summary>
        /// Constructor for initialising the <see cref="IAlphaModel"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">>Model that generates alpha</param>
        public AlphaModelPythonWrapper(PyObject model)
        {
            _model = new BasePythonWrapper<AlphaModel>(model, false);
            foreach (var attributeName in new[] { "Update", "OnSecuritiesChanged" })
            {
                if (!_model.HasAttr(attributeName))
                {
                    throw new NotImplementedException($"IAlphaModel.{attributeName} must be implemented. Please implement this missing method on {model.GetPythonType()}");
                }
            }
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            return _model.InvokeMethodAndEnumerate<Insight>(nameof(Update), algorithm, data);
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            _model.InvokeVoidMethod(nameof(OnSecuritiesChanged), algorithm, changes);
        }
    }
}
