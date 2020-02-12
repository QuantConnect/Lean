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
    /// specifying a custom rebalance function that returns null in some cases, see GH 4075.
    /// </summary>
    public class PortfolioRebalanceOnCustomFuncRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _lastRebalanceTime;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2015, 1, 5);
            SetEndDate(2017, 1, 1);

            PortfolioConstructionModel.RebalanceOnInsightChanges = false;

            SetUniverseSelection(new CustomUniverseSelectionModel("CustomUniverseSelectionModel",
                time => new List<string> { "AAPL", "IBM", "FB", "SPY" }
            ));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(
                time =>
                {
                    if (Portfolio.MarginRemaining > 60000
                        && _lastRebalanceTime != time)
                    {
                        _lastRebalanceTime = time;
                        return time;
                    }
                    if (Portfolio.MarginRemaining < 40000
                        && _lastRebalanceTime != time)
                    {
                        _lastRebalanceTime = time;
                        return time;
                    }
                    return null;
                }));
            SetExecution(new ImmediateExecutionModel());
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                if (UtcTime - _lastRebalanceTime > TimeSpan.Zero)
                {
                    throw new Exception($"{UtcTime} {orderEvent.Symbol} {UtcTime - _lastRebalanceTime}");
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "170"},
            {"Average Win", "0.08%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "11.252%"},
            {"Drawdown", "17.700%"},
            {"Expectancy", "35.224"},
            {"Net Profit", "23.625%"},
            {"Sharpe Ratio", "0.636"},
            {"Probabilistic Sharpe Ratio", "28.623%"},
            {"Loss Rate", "3%"},
            {"Win Rate", "97%"},
            {"Profit-Loss Ratio", "36.18"},
            {"Alpha", "0.1"},
            {"Beta", "0.01"},
            {"Annual Standard Deviation", "0.158"},
            {"Annual Variance", "0.025"},
            {"Information Ratio", "0.158"},
            {"Tracking Error", "0.203"},
            {"Treynor Ratio", "10.469"},
            {"Total Fees", "$170.86"},
            {"Fitness Score", "0.001"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "0.917"},
            {"Return Over Maximum Drawdown", "0.635"},
            {"Portfolio Turnover", "0.002"},
            {"Total Insights Generated", "2020"},
            {"Total Insights Closed", "2016"},
            {"Total Insights Analysis Completed", "2016"},
            {"Long Insight Count", "2020"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}
