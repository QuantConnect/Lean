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
    /// when setting 'PortfolioConstructionModel.RebalanceOnSecurityChanges' to false, see GH 4075.
    /// </summary>
    public class PortfolioRebalanceOnSecurityChangesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
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

            Settings.RebalancePortfolioOnSecurityChanges = false;
            Settings.RebalancePortfolioOnInsightChanges = false;

            SetUniverseSelection(new CustomUniverseSelectionModel("CustomUniverseSelectionModel",
                time =>
                {
                    if (new[] { DayOfWeek.Friday, DayOfWeek.Thursday }.Contains(time.DayOfWeek))
                    {
                        return new List<string> { "FB", "SPY" };
                    }
                    return new List<string> { "AAPL", "IBM" };
                }
            ));
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
            {"Total Trades", "74"},
            {"Average Win", "2.45%"},
            {"Average Loss", "-2.29%"},
            {"Compounding Annual Return", "-4.573%"},
            {"Drawdown", "30.400%"},
            {"Expectancy", "-0.081"},
            {"Net Profit", "-8.937%"},
            {"Sharpe Ratio", "-0.149"},
            {"Probabilistic Sharpe Ratio", "3.634%"},
            {"Loss Rate", "56%"},
            {"Win Rate", "44%"},
            {"Profit-Loss Ratio", "1.07"},
            {"Alpha", "-0.026"},
            {"Beta", "0.03"},
            {"Annual Standard Deviation", "0.165"},
            {"Annual Variance", "0.027"},
            {"Information Ratio", "-0.418"},
            {"Tracking Error", "0.208"},
            {"Treynor Ratio", "-0.827"},
            {"Total Fees", "$136.74"},
            {"Fitness Score", "0.027"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "1"},
            {"Sortino Ratio", "-0.323"},
            {"Return Over Maximum Drawdown", "-0.15"},
            {"Portfolio Turnover", "0.06"},
            {"Total Insights Generated", "534"},
            {"Total Insights Closed", "534"},
            {"Total Insights Analysis Completed", "534"},
            {"Long Insight Count", "534"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "0cbe9e7f9c0ac491e382b2953775a0f8"}
        };
    }
}
