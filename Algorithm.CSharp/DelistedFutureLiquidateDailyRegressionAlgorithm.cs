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
    /// Regression algorithm which reproduces GH issue 4446, in the case of daily resolution.
    /// </summary>
    public class DelistedFutureLiquidateDailyRegressionAlgorithm : DelistedFutureLiquidateRegressionAlgorithm
    {
        protected override Resolution Resolution => Resolution.Daily;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 1237;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "8.18%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "40.865%"},
            {"Drawdown", "1.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "108179.1"},
            {"Net Profit", "8.179%"},
            {"Sharpe Ratio", "3.949"},
            {"Sortino Ratio", "6.572"},
            {"Probabilistic Sharpe Ratio", "94.317%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.041"},
            {"Beta", "0.774"},
            {"Annual Standard Deviation", "0.067"},
            {"Annual Variance", "0.005"},
            {"Information Ratio", "-4.972"},
            {"Tracking Error", "0.026"},
            {"Treynor Ratio", "0.343"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$140000000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "1.98%"},
            {"Drawdown Recovery", "14"},
            {"OrderListHash", "5cb101c65356e52ff42296ef0adff743"}
        };
    }
}
