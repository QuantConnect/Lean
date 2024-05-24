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
    /// specifying a date rules, see GH 4075.
    /// </summary>
    public class PortfolioRebalanceOnDateRulesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

            // let's use 0 minimum order margin percentage so we can assert trades are only submitted immediately after rebalance on Wednesday
            // if not, due to TPV variations happening every day we might no cross the minimum on wednesday but yes another day of the week
            Settings.MinimumOrderMarginPortfolioPercentage = 0m;

            SetStartDate(2015, 1, 1);
            SetEndDate(2017, 1, 1);

            Settings.RebalancePortfolioOnInsightChanges = false;
            Settings.RebalancePortfolioOnSecurityChanges = false;

            SetUniverseSelection(new CustomUniverseSelectionModel(
                "CustomUniverseSelectionModel",
                time => new List<string> { "AAPL", "IBM", "FB", "SPY" }
            ));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(DateRules.Every(DayOfWeek.Wednesday)));
            SetExecution(new ImmediateExecutionModel());
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                Debug($"{orderEvent}");
                if (UtcTime.DayOfWeek != DayOfWeek.Wednesday)
                {
                    throw new Exception($"{UtcTime} {orderEvent.Symbol} {UtcTime.DayOfWeek}");
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

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
            {"Total Orders", "359"},
            {"Average Win", "0.06%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "11.414%"},
            {"Drawdown", "18.200%"},
            {"Expectancy", "1.289"},
            {"Start Equity", "100000"},
            {"End Equity", "124130.05"},
            {"Net Profit", "24.130%"},
            {"Sharpe Ratio", "0.563"},
            {"Sortino Ratio", "0.662"},
            {"Probabilistic Sharpe Ratio", "24.886%"},
            {"Loss Rate", "23%"},
            {"Win Rate", "77%"},
            {"Profit-Loss Ratio", "1.98"},
            {"Alpha", "0.036"},
            {"Beta", "1.019"},
            {"Annual Standard Deviation", "0.141"},
            {"Annual Variance", "0.02"},
            {"Information Ratio", "0.505"},
            {"Tracking Error", "0.072"},
            {"Treynor Ratio", "0.078"},
            {"Total Fees", "$363.83"},
            {"Estimated Strategy Capacity", "$71000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.33%"},
            {"OrderListHash", "fbc4fc832ebbd969bd449d38886575c0"}
        };
    }
}
