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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Alphas.Analysis;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm showing how to define a custom insight scoring function and using the insight manager
    /// </summary>
    public class InsightScoringRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SetUniverseSelection(new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Resolution.Daily));
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));

            // we specify a custom insight score function
            Insights.SetInsightScoreFunction(new CustomInsightScoreFunction(Securities));
        }

        public override void OnEndOfAlgorithm()
        {
            var allInsights = Insights.GetInsights(insight => true);

            if(allInsights.Count != 100 || Insights.GetInsights().Count != 100)
            {
                throw new Exception($"Unexpected insight count found {allInsights.Count}");
            }

            if(allInsights.Count(insight => insight.Score.Magnitude == 0 || insight.Score.Direction == 0) < 5)
            {
                throw new Exception($"Insights not scored!");
            }

            if (allInsights.Count(insight => insight.Score.IsFinalScore) < 99)
            {
                throw new Exception($"Insights not finalized!");
            }
        }

        private class CustomInsightScoreFunction : IInsightScoreFunction
        {
            private readonly Dictionary<Guid, Insight> _openInsights = new();
            private SecurityManager _securities;

            public CustomInsightScoreFunction(SecurityManager securities)
            {
                _securities = securities;
            }

            public void Score(InsightManager insightManager, DateTime utcTime)
            {
                var openInsights = insightManager.GetActiveInsights(utcTime);

                foreach (var insight in openInsights)
                {
                    _openInsights[insight.Id] = insight;
                }

                List<Insight> toRemove = new();
                foreach (var kvp in _openInsights)
                {
                    var openInsight = kvp.Value;

                    var security = _securities[openInsight.Symbol];
                    openInsight.ReferenceValueFinal = security.Price;

                    var score = openInsight.ReferenceValueFinal - openInsight.ReferenceValue;
                    openInsight.Score.SetScore(InsightScoreType.Direction, (double)score, utcTime);
                    openInsight.Score.SetScore(InsightScoreType.Magnitude, (double)score * 2, utcTime);
                    openInsight.EstimatedValue = score * 100;

                    if (openInsight.IsExpired(utcTime))
                    {
                        openInsight.Score.Finalize(utcTime);
                        toRemove.Add(openInsight);
                    }
                }

                // clean up
                foreach (var insightToRemove in toRemove)
                {
                    _openInsights.Remove(insightToRemove.Id);
                }
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
