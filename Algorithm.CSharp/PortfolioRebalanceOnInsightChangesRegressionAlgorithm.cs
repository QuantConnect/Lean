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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing portfolio construction model control over rebalancing,
    /// when setting 'PortfolioConstructionModel.RebalanceOnInsightChanges' to false, see GH 4075.
    /// </summary>
    public class PortfolioRebalanceOnInsightChangesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Symbol, DateTime> _lastOrderFilled;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2015, 1, 1);
            SetEndDate(2017, 1, 1);

            Settings.RebalancePortfolioOnInsightChanges = false;

            SetUniverseSelection(new CustomUniverseSelectionModel("CustomUniverseSelectionModel",
                time => new List<string> { "FB", "SPY", "AAPL", "IBM" }));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(
                time => time.AddDays(30)));
            SetExecution(new ImmediateExecutionModel());

            _lastOrderFilled = new Dictionary<Symbol, DateTime>();
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                DateTime lastOrderFilled;
                if (_lastOrderFilled.TryGetValue(orderEvent.Symbol, out lastOrderFilled))
                {
                    if (UtcTime - lastOrderFilled < TimeSpan.FromDays(30))
                    {
                        throw new Exception($"{UtcTime} {orderEvent.Symbol} {UtcTime - lastOrderFilled}");
                    }
                }
                _lastOrderFilled[orderEvent.Symbol] = UtcTime;

                Debug($"{orderEvent}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "83"},
            {"Average Win", "0.15%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "9.870%"},
            {"Drawdown", "18.200%"},
            {"Expectancy", "2.432"},
            {"Net Profit", "20.715%"},
            {"Sharpe Ratio", "0.605"},
            {"Probabilistic Sharpe Ratio", "25.624%"},
            {"Loss Rate", "18%"},
            {"Win Rate", "82%"},
            {"Profit-Loss Ratio", "3.17"},
            {"Alpha", "0.093"},
            {"Beta", "0.012"},
            {"Annual Standard Deviation", "0.155"},
            {"Annual Variance", "0.024"},
            {"Information Ratio", "0.157"},
            {"Tracking Error", "0.201"},
            {"Treynor Ratio", "8.122"},
            {"Total Fees", "$83.80"},
            {"Fitness Score", "0.001"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "0.818"},
            {"Return Over Maximum Drawdown", "0.543"},
            {"Portfolio Turnover", "0.002"},
            {"Total Insights Generated", "2028"},
            {"Total Insights Closed", "2024"},
            {"Total Insights Analysis Completed", "2024"},
            {"Long Insight Count", "2028"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "ede3036c069f5eb66aaa27ac2f9b8747"}
        };
    }
}
