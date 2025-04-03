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
    /// This regression algorithm tests In The Money (ITM) index option expiry for calls using daily resolution.
    /// </summary>
    public class IndexOptionCallITMExpiryDailyRegressionAlgorithm : IndexOptionCallITMExpiryRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        public override void Initialize()
        {
            Settings.DailyPreciseEndTime = true;
            base.Initialize();
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 195;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 1;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "10.27%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "301.565%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "110274"},
            {"Net Profit", "10.274%"},
            {"Sharpe Ratio", "5.291"},
            {"Sortino Ratio", "384.846"},
            {"Probabilistic Sharpe Ratio", "88.621%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "1.833"},
            {"Beta", "-0.228"},
            {"Annual Standard Deviation", "0.345"},
            {"Annual Variance", "0.119"},
            {"Information Ratio", "4.653"},
            {"Tracking Error", "0.383"},
            {"Treynor Ratio", "-7.99"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P3GHDZXQ|SPX 31"},
            {"Portfolio Turnover", "1.90%"},
            {"OrderListHash", "fce4ce6f25578a0ec8e7efa272b2dd02"}
        };
    }
}
