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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Python;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class PortfolioConstructionModelPythonWrapper : PortfolioConstructionModel
    {
        private readonly BasePythonWrapper<PortfolioConstructionModel> _model;
        private readonly bool _implementsDetermineTargetPercent;

        /// <summary>
        /// True if should rebalance portfolio on security changes. True by default
        /// </summary>
        public override bool RebalanceOnSecurityChanges
        {
            get
            {
                return _model.GetProperty<bool>(nameof(RebalanceOnSecurityChanges));
            }
            set
            {
                _model.SetProperty(nameof(RebalanceOnSecurityChanges), value);
            }
        }

        /// <summary>
        /// True if should rebalance portfolio on new insights or expiration of insights. True by default
        /// </summary>
        public override bool RebalanceOnInsightChanges
        {
            get
            {
                return _model.GetProperty<bool>(nameof(RebalanceOnInsightChanges));
            }
            set
            {
                _model.SetProperty(nameof(RebalanceOnInsightChanges), value);
            }
        }

        /// <summary>
        /// Constructor for initialising the <see cref="IPortfolioConstructionModel"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Model defining how to build a portfolio from alphas</param>
        public PortfolioConstructionModelPythonWrapper(PyObject model)
        {
            _model = new BasePythonWrapper<PortfolioConstructionModel>(model, false);
            using (Py.GIL())
            {
                foreach (var attributeName in new[] { "CreateTargets", "OnSecuritiesChanged" })
                {
                    if (!_model.HasAttr(attributeName))
                    {
                        throw new NotImplementedException($"IPortfolioConstructionModel.{attributeName} must be implemented. Please implement this missing method on {model.GetPythonType()}");
                    }
                }

                _model.InvokeMethod(nameof(SetPythonWrapper), this).Dispose();

                _implementsDetermineTargetPercent = model.GetPythonMethod("DetermineTargetPercent") != null;
            }
        }

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <returns>An enumerable of portfolio targets to be sent to the execution model</returns>
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            return _model.InvokeMethod<IEnumerable<IPortfolioTarget>>(nameof(CreateTargets), algorithm, insights);
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

        /// <summary>
        /// Method that will determine if the portfolio construction model should create a
        /// target for this insight
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the portfolio should create a target for the insight</returns>
        protected override bool ShouldCreateTargetForInsight(Insight insight)
        {
            return _model.InvokeMethod<bool>(nameof(ShouldCreateTargetForInsight), insight);
        }

        /// <summary>
        /// Determines if the portfolio should be rebalanced base on the provided rebalancing func,
        /// if any security change have been taken place or if an insight has expired or a new insight arrived
        /// If the rebalancing function has not been provided will return true.
        /// </summary>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <param name="algorithmUtc">The current algorithm UTC time</param>
        /// <returns>True if should rebalance</returns>
        protected override bool IsRebalanceDue(Insight[] insights, DateTime algorithmUtc)
        {
            return _model.InvokeMethod<bool>(nameof(IsRebalanceDue), insights, algorithmUtc);
        }

        /// <summary>
        /// Gets the target insights to calculate a portfolio target percent for
        /// </summary>
        /// <returns>An enumerable of the target insights</returns>
        protected override List<Insight> GetTargetInsights()
        {
            return _model.InvokeMethod<List<Insight>>(nameof(GetTargetInsights));
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            using (Py.GIL())
            {
                if (!_implementsDetermineTargetPercent)
                {
                    // the implementation is in C#
                    return _model.InvokeMethod<Dictionary<Insight, double>>(nameof(DetermineTargetPercent), activeInsights);
                }

                Dictionary<Insight, double> dic;
                var result = _model.InvokeMethod(nameof(DetermineTargetPercent), activeInsights) as dynamic;
                if ((result as PyObject).TryConvert(out dic))
                {
                    // this is required if the python implementation is actually returning a C# dic, not common,
                    // but could happen if its actually calling a base C# implementation
                    return dic;
                }

                dic = new Dictionary<Insight, double>();
                foreach (var pyInsight in result)
                {
                    var insight = (pyInsight as PyObject).As<Insight>();
                    dic[insight] = result[pyInsight];
                }

                return dic;
            }
        }
    }
}
