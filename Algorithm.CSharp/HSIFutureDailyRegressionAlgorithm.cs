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
            {"Average Win", "14.99%"},
            {"Average Loss", "-21.17%"},
            {"Compounding Annual Return", "-99.870%"},
            {"Drawdown", "38.200%"},
            {"Expectancy", "-0.146"},
            {"Start Equity", "100000"},
            {"End Equity", "81850"},
            {"Net Profit", "-18.150%"},
            {"Sharpe Ratio", "-0.482"},
            {"Sortino Ratio", "-0.495"},
            {"Probabilistic Sharpe Ratio", "31.192%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.71"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "1.998"},
            {"Annual Variance", "3.991"},
            {"Information Ratio", "-0.479"},
            {"Tracking Error", "1.998"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$360.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "HSI VL6DN7UV65S9"},
            {"Portfolio Turnover", "1267.84%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "26c1d57ab4f1af0d5b1fbc380431ccf5"}
        };
    }
}
