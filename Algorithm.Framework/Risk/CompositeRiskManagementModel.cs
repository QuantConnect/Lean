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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Risk
{
    /// <summary>
    /// Provides an implementation of <see cref="IRiskManagementModel"/> that combines multiple risk
    /// models into a single risk management model and properly sets each insights 'SourceModel' property.
    /// </summary>
    public class CompositeRiskManagementModel : RiskManagementModel
    {
        private readonly IRiskManagementModel[] _riskManagementModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRiskManagementModel"/> class
        /// </summary>
        /// <param name="riskManagementModels">The individual risk management models defining this composite model</param>
        public CompositeRiskManagementModel(params IRiskManagementModel[] riskManagementModels)
        {
            if (riskManagementModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 risk management model for the CompositeRiskManagementModel");
            }

            _riskManagementModels = riskManagementModels;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRiskManagementModel"/> class
        /// </summary>
        /// <param name="riskManagementModels">The individual risk management models defining this composite model</param>
        public CompositeRiskManagementModel(IEnumerable<IRiskManagementModel>riskManagementModels)
        {
            _riskManagementModels = riskManagementModels?.ToArray();

            if (_riskManagementModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 risk management model for the CompositeRiskManagementModel");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRiskManagementModel"/> class
        /// </summary>
        /// <param name="riskManagementModels">The individual risk management models defining this composite model</param>
        public CompositeRiskManagementModel(params PyObject[] riskManagementModels)
        {
            if (riskManagementModels.IsNullOrEmpty())
            {
                throw new ArgumentException("Must specify at least 1 risk management model for the CompositeRiskManagementModel");
            }

            _riskManagementModels = new IRiskManagementModel[riskManagementModels.Length];

            for (var i = 0; i < riskManagementModels.Length; i++)
            {
                if (riskManagementModels[i].TryConvert(out _riskManagementModels[i]))
                {
                    _riskManagementModels[i] = new RiskManagementModelPythonWrapper(riskManagementModels[i]);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeRiskManagementModel"/> class
        /// </summary>
        /// <param name="riskManagementModel">The individual risk management model defining this composite model</param>
        public CompositeRiskManagementModel(PyObject riskManagementModel)
            : this(new[] { riskManagementModel} )
        {

        }

        /// <summary>
        /// Manages the algorithm's risk at each time step.
        /// This method patches this call through the each of the wrapped models.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="targets">The current portfolio targets to be assessed for risk</param>
        /// <returns>The new portfolio targets</returns>
        public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithmFramework algorithm, IPortfolioTarget[] targets)
        {
            if (targets.Length > 0)
            {
                foreach (var model in _riskManagementModels)
                {
                    // take into account the possibility of ManageRisk returning nothing
                    var riskAdjusted = model.ManageRisk(algorithm, targets);

                    // produce a distinct set of new targets giving preference to newer targets
                    targets = riskAdjusted.Concat(targets).DistinctBy(t => t.Symbol).ToArray();
                }
            }

            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed.
        /// This method patches this call through the each of the wrapped models.
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            foreach (var model in _riskManagementModels)
            {
                model.OnSecuritiesChanged(algorithm, changes);
            }
        }
    }
}
