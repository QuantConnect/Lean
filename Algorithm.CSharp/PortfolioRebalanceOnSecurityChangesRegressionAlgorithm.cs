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
                        throw new RegressionTestException($"{UtcTime} {orderEvent.Symbol} {UtcTime - lastOrderFilled}");
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
                throw new RegressionTestException($"The number of insights in the insight manager should be different of the number of all insights generated ({_generatedInsightsCount})");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5485;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "64"},
            {"Average Win", "2.71%"},
            {"Average Loss", "-2.34%"},
            {"Compounding Annual Return", "2.256%"},
            {"Drawdown", "25.500%"},
            {"Expectancy", "0.079"},
            {"Start Equity", "100000"},
            {"End Equity", "104560.59"},
            {"Net Profit", "4.561%"},
            {"Sharpe Ratio", "0.117"},
            {"Sortino Ratio", "0.106"},
            {"Probabilistic Sharpe Ratio", "8.398%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "1.16"},
            {"Alpha", "-0.01"},
            {"Beta", "0.569"},
            {"Annual Standard Deviation", "0.125"},
            {"Annual Variance", "0.016"},
            {"Information Ratio", "-0.243"},
            {"Tracking Error", "0.117"},
            {"Treynor Ratio", "0.026"},
            {"Total Fees", "$271.25"},
            {"Estimated Strategy Capacity", "$44000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "4.37%"},
            {"OrderListHash", "d6286db83c9d034251491fae4c937d76"}
        };
    }
}
