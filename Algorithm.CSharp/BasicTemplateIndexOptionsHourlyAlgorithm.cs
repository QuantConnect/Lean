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
    /// Regression for running an IndexOptions algorithm with Hourly data
    /// </summary>
    public class BasicTemplateIndexOptionsHourlyAlgorithm : BasicTemplateIndexOptionsDailyAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 1269;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "81"},
            {"Average Win", "0.00%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-0.006%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-0.486"},
            {"Start Equity", "1000000"},
            {"End Equity", "999995"},
            {"Net Profit", "0.000%"},
            {"Sharpe Ratio", "-101.77"},
            {"Sortino Ratio", "-9053542.758"},
            {"Probabilistic Sharpe Ratio", "17.439%"},
            {"Loss Rate", "97%"},
            {"Win Rate", "3%"},
            {"Profit-Loss Ratio", "17.50"},
            {"Alpha", "-0.003"},
            {"Beta", "-0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.449"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "116.921"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P59H5E6M|SPX 31"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "1c5f424cfe62777733ee68a20320bb8d"}
        };
    }
}
