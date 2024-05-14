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
    /// This regression algorithm tests Out of The Money (OTM) index option expiry for calls using daily resolution.
    /// </summary>
    public class IndexOptionCallOTMExpiryDailyRegressionAlgorithm : IndexOptionCallOTMExpiryRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        public override void Initialize()
        {
            Settings.DailyStrictEndTimeEnabled = true;
            base.Initialize();
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 184;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.142%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99990"},
            {"Net Profit", "-0.010%"},
            {"Sharpe Ratio", "-15.959"},
            {"Sortino Ratio", "-124989.863"},
            {"Probabilistic Sharpe Ratio", "0.015%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.004"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.334"},
            {"Tracking Error", "0.138"},
            {"Treynor Ratio", "-32.969"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "SPX XL80P59H5E6M|SPX 31"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "3cfa774d70e5d7e9dcd5e56a047d7c80"}
        };
    }
}
