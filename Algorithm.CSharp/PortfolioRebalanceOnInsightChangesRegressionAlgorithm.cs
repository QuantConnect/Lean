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

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 6076;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "78"},
            {"Average Win", "0.17%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "9.833%"},
            {"Drawdown", "18.100%"},
            {"Expectancy", "2.369"},
            {"Start Equity", "100000"},
            {"End Equity", "120633.71"},
            {"Net Profit", "20.634%"},
            {"Sharpe Ratio", "0.486"},
            {"Sortino Ratio", "0.573"},
            {"Probabilistic Sharpe Ratio", "21.159%"},
            {"Loss Rate", "19%"},
            {"Win Rate", "81%"},
            {"Profit-Loss Ratio", "3.15"},
            {"Alpha", "0.024"},
            {"Beta", "1.033"},
            {"Annual Standard Deviation", "0.142"},
            {"Annual Variance", "0.02"},
            {"Information Ratio", "0.364"},
            {"Tracking Error", "0.071"},
            {"Treynor Ratio", "0.067"},
            {"Total Fees", "$82.57"},
            {"Estimated Strategy Capacity", "$83000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.23%"},
            {"OrderListHash", "24ba77ccc12b501ad112419bd7ea4f99"}
        };
    }
}
