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
        public override long DataPoints => 174;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 12;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public override AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "15"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.33%"},
            {"Compounding Annual Return", "-55.187%"},
            {"Drawdown", "2.400%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "97610"},
            {"Net Profit", "-2.390%"},
            {"Sharpe Ratio", "-15.799"},
            {"Sortino Ratio", "-19.207"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.029"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-15.544"},
            {"Tracking Error", "0.029"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$600.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "HSI VL6DN7UV65S9"},
            {"Portfolio Turnover", "1590.77%"},
            {"OrderListHash", "46fc4362ac20b63ea361ff8d8ad38d90"}
        };
    }
}
