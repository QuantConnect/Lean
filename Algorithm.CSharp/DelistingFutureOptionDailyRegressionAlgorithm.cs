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
    /// Regression algorithm reproducing issue #5160 where delisting order would be cancelled because it was placed at the market close on the delisting day,
    /// in the case of daily resolution.
    /// </summary>
    public class DelistingFutureOptionDailyRegressionAlgorithm : DelistingFutureOptionRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 1380;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0.00%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "-0.134%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-0.429"},
            {"Start Equity", "10000000"},
            {"End Equity", "9997036.08"},
            {"Net Profit", "-0.030%"},
            {"Sharpe Ratio", "-31.58"},
            {"Sortino Ratio", "-7.862"},
            {"Probabilistic Sharpe Ratio", "2.982%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.14"},
            {"Alpha", "-0.02"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "1.511"},
            {"Tracking Error", "0.429"},
            {"Treynor Ratio", "-84.756"},
            {"Total Fees", "$1.42"},
            {"Estimated Strategy Capacity", "$540000000.00"},
            {"Lowest Capacity Asset", "ES XCZJLDRF238K|ES XCZJLC9NOB29"},
            {"Portfolio Turnover", "0.04%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "11ac76bc66928a5e97e064281f6e7ef5"}
        };
    }
}
