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
    /// Specific for hour resolution.
    /// </summary>
    public class EquitySplitHoldingsHourRegressionAlgorithm : EquitySplitHoldingsMinuteRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 80;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-57.230%"},
            {"Drawdown", "2.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98460.68"},
            {"Net Profit", "-1.539%"},
            {"Sharpe Ratio", "-3.994"},
            {"Sortino Ratio", "-4.265"},
            {"Probabilistic Sharpe Ratio", "11.588%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.447"},
            {"Beta", "-0.254"},
            {"Annual Standard Deviation", "0.118"},
            {"Annual Variance", "0.014"},
            {"Information Ratio", "-4.414"},
            {"Tracking Error", "0.129"},
            {"Treynor Ratio", "1.854"},
            {"Total Fees", "$1.00"},
            {"Estimated Strategy Capacity", "$68000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "14.24%"},
            {"OrderListHash", "2dfd8b59a3479fdfc7535ed1564dcfc3"}
        };
    }
}
