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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the current price of the security is adjusted after a split.
    /// Specific for daily resolution.
    /// </summary>
    public class EquitySplitHoldingsDailyRegressionAlgorithm : EquitySplitHoldingsMinuteRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 50;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-44.829%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98919.60"},
            {"Net Profit", "-1.080%"},
            {"Sharpe Ratio", "-2.862"},
            {"Sortino Ratio", "-3.355"},
            {"Probabilistic Sharpe Ratio", "23.493%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.395"},
            {"Beta", "0.265"},
            {"Annual Standard Deviation", "0.129"},
            {"Annual Variance", "0.017"},
            {"Information Ratio", "-3.531"},
            {"Tracking Error", "0.132"},
            {"Treynor Ratio", "-1.392"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$370000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.20%"},
            {"OrderListHash", "6ea9aed8840dc08f9290028271750dcf"}
        };
    }
}
