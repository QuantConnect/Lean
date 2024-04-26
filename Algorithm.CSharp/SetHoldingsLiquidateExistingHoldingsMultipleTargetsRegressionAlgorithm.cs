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

using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm testing GH feature 3790, using SetHoldings with a collection of targets
    /// which will be ordered by margin impact before being executed, with the objective of avoiding any
    /// margin errors
    /// Asserts that liquidateExistingHoldings equal false does not close positions inadvertedly (GH 7008) 
    /// </summary>
    public class SetHoldingsLiquidateExistingHoldingsMultipleTargetsRegressionAlgorithm : SetHoldingsMultipleTargetsRegressionAlgorithm
    {
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(new List<PortfolioTarget> { new("SPY", 0.8m), new("IBM", 0.2m) },
                    liquidateExistingHoldings: true);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new()
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "300.863%"},
            {"Drawdown", "2.100%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101791.04"},
            {"Net Profit", "1.791%"},
            {"Sharpe Ratio", "9.746"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.282%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.21"},
            {"Beta", "0.995"},
            {"Annual Standard Deviation", "0.223"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "7.104"},
            {"Tracking Error", "0.028"},
            {"Treynor Ratio", "2.186"},
            {"Total Fees", "$3.76"},
            {"Estimated Strategy Capacity", "$40000000.00"},
            {"Lowest Capacity Asset", "IBM R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.93%"},
            {"OrderListHash", "b56516b18612ece304dfd566f8b2e2f6"}
        };
    }
}
