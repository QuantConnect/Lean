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

using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing the effect of <see cref="IAlgorithmSettings.MinimumOrderMarginPortfolioPercentage"/>.
    /// Setting a minimum order size of 1% of portfolio reduces order count significantly
    /// </summary>
    public class MinimumOrderMarginRegressionAlgorithm : NoMinimumOrderMarginRegressionAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            Settings.MinimumOrderMarginPortfolioPercentage = 0.01m;
        }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "39.100%"},
            {"Drawdown", "0.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100422.84"},
            {"Net Profit", "0.423%"},
            {"Sharpe Ratio", "5.498"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.498%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.186"},
            {"Beta", "0.248"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-9.989"},
            {"Tracking Error", "0.167"},
            {"Treynor Ratio", "1.223"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$150000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "4.98%"},
            {"OrderListHash", "8774049eb5141a2b6956d9432426f837"}
        };
    }
}
