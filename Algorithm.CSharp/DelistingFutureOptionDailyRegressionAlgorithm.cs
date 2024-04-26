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
using QuantConnect.Orders;

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
        public override long DataPoints => 13091;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "16"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-0.111%"},
            {"Drawdown", "0.100%"},
            {"Expectancy", "-0.678"},
            {"Start Equity", "10000000"},
            {"End Equity", "9988860.24"},
            {"Net Profit", "-0.111%"},
            {"Sharpe Ratio", "-10.266"},
            {"Sortino Ratio", "-0.941"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "80%"},
            {"Win Rate", "20%"},
            {"Profit-Loss Ratio", "0.61"},
            {"Alpha", "-0.008"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.001"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.073"},
            {"Tracking Error", "0.107"},
            {"Treynor Ratio", "14.418"},
            {"Total Fees", "$19.76"},
            {"Estimated Strategy Capacity", "$2400000.00"},
            {"Lowest Capacity Asset", "DC V5E8PHPRCHJ8|DC V5E8P9SH0U0X"},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", "9521f1fae55a3ffab939592b9ae04855"}
        };
    }
}
