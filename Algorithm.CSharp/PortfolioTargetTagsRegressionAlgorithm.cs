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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm demonstrating the portfolio target tags usage
    /// </summary>
    public class PortfolioTargetTagsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _targetsTagChecked;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));

            SetPortfolioConstruction(new CustomPortfolioConstructionModel());
            SetExecution(new CustomExecutionModel(() => _targetsTagChecked = true));
            SetRiskManagement(new CustomRiskManagementModel());
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_targetsTagChecked)
            {
                throw new Exception("The portfolio targets tag were not checked");
            }
        }

        private class CustomPortfolioConstructionModel : EqualWeightingPortfolioConstructionModel
        {
            public CustomPortfolioConstructionModel() : base(Resolution.Daily)
            {
            }

            public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
            {
                var targets = base.CreateTargets(algorithm, insights);
                foreach (var target in targets)
                {
                    yield return new PortfolioTarget(target.Symbol, target.Quantity, tag: GeneratePortfolioTargetTag(target));
                }
            }

            public static string GeneratePortfolioTargetTag(IPortfolioTarget target)
            {
                return $"Portfolio target tag: {target.Symbol} - {target.Quantity}";
            }
        }

        private class CustomRiskManagementModel : MaximumDrawdownPercentPerSecurity
        {
            public CustomRiskManagementModel() : base(0.01m)
            {
            }

            public override IEnumerable<IPortfolioTarget> ManageRisk(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                var riskManagedTargets = base.ManageRisk(algorithm, targets);
                foreach (var target in riskManagedTargets)
                {
                    yield return new PortfolioTarget(target.Symbol, target.Quantity,
                        tag: CustomPortfolioConstructionModel.GeneratePortfolioTargetTag(target));
                }
            }
        }

        private class CustomExecutionModel : ImmediateExecutionModel
        {
            private Action _targetsTagCheckedCallback;

            public CustomExecutionModel(Action targetsTagCheckedCallback)
            {
                _targetsTagCheckedCallback = targetsTagCheckedCallback;
            }

            public override void Execute(QCAlgorithm algorithm, IPortfolioTarget[] targets)
            {
                if (targets.Length > 0)
                {
                    _targetsTagCheckedCallback();
                }

                foreach (var target in targets)
                {
                    var expectedTag = CustomPortfolioConstructionModel.GeneratePortfolioTargetTag(target);
                    if (target.Tag != expectedTag)
                    {
                        throw new Exception($"Unexpected portfolio target tag: {target.Tag} - Expected: {expectedTag}");
                    }
                }

                base.Execute(algorithm, targets);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.01%"},
            {"Compounding Annual Return", "261.134%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "101655.30"},
            {"Net Profit", "1.655%"},
            {"Sharpe Ratio", "8.472"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "66.840%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.091"},
            {"Beta", "1.006"},
            {"Annual Standard Deviation", "0.224"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "-33.445"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "1.885"},
            {"Total Fees", "$10.32"},
            {"Estimated Strategy Capacity", "$27000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.86%"},
            {"OrderListHash", "f209ed42701b0419858e0100595b40c0"}
        };
    }
}
