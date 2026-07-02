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
 *
*/

using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using and testing HSI futures and index
    /// </summary>
    public class HSIFutureDailyRegressionAlgorithm : HSIFutureHourRegressionAlgorithm
    {
        /// <summary>
        /// The data resolution
        /// </summary>
        protected override Resolution Resolution => Resolution.Daily;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 176;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 115;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public override AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "9"},
            {"Average Win", "5.96%"},
            {"Average Loss", "-0.31%"},
            {"Compounding Annual Return", "381.391%"},
            {"Drawdown", "1.000%"},
            {"Expectancy", "4.085"},
            {"Start Equity", "100000"},
            {"End Equity", "104850"},
            {"Net Profit", "4.850%"},
            {"Sharpe Ratio", "7.838"},
            {"Sortino Ratio", "84.897"},
            {"Probabilistic Sharpe Ratio", "82.546%"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "19.34"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.31"},
            {"Annual Variance", "0.096"},
            {"Information Ratio", "7.862"},
            {"Tracking Error", "0.31"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$360.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "HSI VL6DN7UV65S9"},
            {"Portfolio Turnover", "924.33%"},
            {"Drawdown Recovery", "7"},
            {"OrderListHash", "4b71c15c23ca13ea605602cd9a93f309"}
        };
    }
}
