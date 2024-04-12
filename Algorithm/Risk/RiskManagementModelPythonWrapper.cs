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
using QuantConnect.Python;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class RiskManagementModelPythonWrapper : RiskManagementModel
    {
        private readonly BasePythonWrapper<IRiskManagementModel> _model;

        /// <summary>
        /// Constructor for initialising the <see cref="IRiskManagementModel"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Model defining how risk is managed</param>
        public RiskManagementModelPythonWrapper(PyObject model)
        {
            _model = new BasePythonWrapper<IRiskManagementModel>(model);
        }

        /// <summary>
        /// Manages the algorithm's risk at each time step
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
        {
            using (Py.GIL())
            {
                var riskTargetOverrides = _model.InvokeMethod("ManageRisk", algorithm, targets);
                var iterator = riskTargetOverrides.GetIterator();
                foreach (PyObject target in iterator)
                {
                    yield return target.GetAndDispose<IPortfolioTarget>();
                }
                iterator.Dispose();
                riskTargetOverrides.Dispose();
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            _model.InvokeMethod(nameof(OnSecuritiesChanged), algorithm, changes).Dispose();
        }
    }
}
