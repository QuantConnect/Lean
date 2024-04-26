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
            var freePortfolioValue = Portfolio.TotalPortfolioValue - Portfolio.TotalPortfolioValueLessFreeBuffer;
            if (freePortfolioValue != Portfolio.TotalPortfolioValue * Settings.FreePortfolioValuePercentage)
            {
                throw new Exception($"Unexpected FreePortfolioValue value: {freePortfolioValue}");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 20812;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0.06%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "8.173%"},
            {"Drawdown", "55.100%"},
            {"Expectancy", "2.639"},
            {"Start Equity", "1000000"},
            {"End Equity", "2254609.41"},
            {"Net Profit", "125.461%"},
            {"Sharpe Ratio", "0.36"},
            {"Sortino Ratio", "0.365"},
            {"Probabilistic Sharpe Ratio", "1.164%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "6.28"},
            {"Alpha", "-0"},
            {"Beta", "0.998"},
            {"Annual Standard Deviation", "0.164"},
            {"Annual Variance", "0.027"},
            {"Information Ratio", "-0.192"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "0.059"},
            {"Total Fees", "$45.46"},
            {"Estimated Strategy Capacity", "$190000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.03%"},
            {"OrderListHash", "97f4131cf8573eaabfab2de9e35c4101"}
        };
    }
}
