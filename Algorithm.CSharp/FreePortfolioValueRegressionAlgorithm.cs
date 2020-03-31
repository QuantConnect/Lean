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
    /// Regression algorithm which reproduced GH issue 3759 (performing 26 trades).
    /// </summary>
    public class FreePortfolioValueRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2007, 10, 1);
            SetEndDate(2018, 2, 1);
            SetCash(1000000);

            UniverseSettings.Leverage = 1;
            SetUniverseSelection(
                new ManualUniverseSelectionModel(QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA))
            );
            SetAlpha(
                new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, QuantConnect.Time.OneDay, 0.025, null)
            );
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
        }

        public override void OnEndOfAlgorithm()
        {
            if (Settings.FreePortfolioValue != 1000000 * Settings.FreePortfolioValuePercentage)
            {
                throw new Exception($"Unexpected FreePortfolioValue value: {Settings.FreePortfolioValue}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
             Debug($"OnOrderEvent: {orderEvent}");
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
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "8.172%"},
            {"Drawdown", "55.100%"},
            {"Expectancy", "-1"},
            {"Net Profit", "125.441%"},
            {"Sharpe Ratio", "0.468"},
            {"Probabilistic Sharpe Ratio", "2.566%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.093"},
            {"Beta", "-0.099"},
            {"Annual Standard Deviation", "0.18"},
            {"Annual Variance", "0.032"},
            {"Information Ratio", "-0.001"},
            {"Tracking Error", "0.267"},
            {"Treynor Ratio", "-0.849"},
            {"Total Fees", "$41.17"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "39.01"},
            {"Kelly Criterion Probability Value", "0.009"},
            {"Sortino Ratio", "0.501"},
            {"Return Over Maximum Drawdown", "0.148"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "2604"},
            {"Total Insights Closed", "2603"},
            {"Total Insights Analysis Completed", "2603"},
            {"Long Insight Count", "2604"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$895373.5325"},
            {"Total Accumulated Estimated Alpha Value", "$112733745.6112"},
            {"Mean Population Estimated Insight Value", "$43309.1608"},
            {"Mean Population Direction", "43.2194%"},
            {"Mean Population Magnitude", "43.2194%"},
            {"Rolling Averaged Population Direction", "48.8488%"},
            {"Rolling Averaged Population Magnitude", "48.8488%"},
            {"OrderListHash", "-1756838669"}
        };
    }
}
