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
        private int _generatedInsightsCount;
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

            InsightsGenerated += (_, e) => _generatedInsightsCount += e.Insights.Length;
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

        public override void OnEndOfAlgorithm()
        {
            if (Insights.Count == _generatedInsightsCount)
            {
                // The number of insights is modified by the Portfolio Construction Model,
                // since it removes expired insights and insights from removed securities 
                throw new Exception($"The number of insights in the insight manager should be different of the number of all insights generated ({_generatedInsightsCount})");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5500;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "74"},
            {"Average Win", "2.37%"},
            {"Average Loss", "-2.26%"},
            {"Compounding Annual Return", "-4.837%"},
            {"Drawdown", "29.800%"},
            {"Expectancy", "-0.089"},
            {"Starting Equity", "100000"},
            {"Ending Equity", "90560.73"},
            {"Net Profit", "-9.439%"},
            {"Sharpe Ratio", "-0.24"},
            {"Sortino Ratio", "-0.234"},
            {"Probabilistic Sharpe Ratio", "2.227%"},
            {"Loss Rate", "56%"},
            {"Win Rate", "44%"},
            {"Profit-Loss Ratio", "1.05"},
            {"Alpha", "-0.066"},
            {"Beta", "0.768"},
            {"Annual Standard Deviation", "0.139"},
            {"Annual Variance", "0.019"},
            {"Information Ratio", "-0.709"},
            {"Tracking Error", "0.108"},
            {"Treynor Ratio", "-0.043"},
            {"Total Fees", "$262.72"},
            {"Estimated Strategy Capacity", "$54000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "5.05%"},
            {"OrderListHash", "53a93b60b26c7d5d4c48123707cd761f"}
        };
    }
}
