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
using QuantConnect.Algorithm.Framework.Portfolio;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting setting a free portfolio value disabled trailing behavior, see GH issue #4104
    /// </summary>
    public class FreePortfolioValueFixedRegressionAlgorithm : FreePortfolioValueRegressionAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Settings.FreePortfolioValue = 500;
        }

        public override void OnEndOfAlgorithm()
        {
            var freePortfolioValue = Portfolio.TotalPortfolioValue - Portfolio.TotalPortfolioValueLessFreeBuffer;
            if (freePortfolioValue != 500)
            {
                throw new Exception($"Unexpected FreePortfolioValue value: {freePortfolioValue}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "8.183%"},
            {"Drawdown", "55.100%"},
            {"Expectancy", "-1"},
            {"Start Equity", "1000000"},
            {"End Equity", "2256717.28"},
            {"Net Profit", "125.672%"},
            {"Sharpe Ratio", "0.36"},
            {"Sortino Ratio", "0.365"},
            {"Probabilistic Sharpe Ratio", "1.163%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0"},
            {"Beta", "0.999"},
            {"Annual Standard Deviation", "0.164"},
            {"Annual Variance", "0.027"},
            {"Information Ratio", "-0.088"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "0.059"},
            {"Total Fees", "$43.54"},
            {"Estimated Strategy Capacity", "$430000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.03%"},
            {"OrderListHash", "13e6d0c7282659fc01b9ffc0f9cebb70"}
        };
    }
}
