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

using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the futures daily cash settlement behavior taking short positions
    /// </summary>
    public class FuturesDailySettlementShortRegressionAlgorithm : FuturesDailySettlementLongRegressionAlgorithm
    {
        /// <summary>
        /// Expected cash balance for each day
        /// </summary>
        protected override Dictionary<DateTime, decimal> ExpectedCash { get; } = new()
        {
            { new DateTime(2013, 10, 07), 100000 },
            { new DateTime(2013, 10, 08), 96701.95m },
            { new DateTime(2013, 10, 09), 98718.55m },
            { new DateTime(2013, 10, 10), 97937.10m },
            { new DateTime(2013, 10, 10, 17, 0, 0), 98943.15m }
        };

        /// <summary>
        /// Order side factor
        /// </summary>
        protected override int OrderSide => -1;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "6"},
            {"Average Win", "0.83%"},
            {"Average Loss", "-0.94%"},
            {"Compounding Annual Return", "-64.858%"},
            {"Drawdown", "47.200%"},
            {"Expectancy", "-0.373"},
            {"Net Profit", "-1.057%"},
            {"Sharpe Ratio", "26.452"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "0.88"},
            {"Alpha", "6.4"},
            {"Beta", "-0.176"},
            {"Annual Standard Deviation", "0.232"},
            {"Annual Variance", "0.054"},
            {"Information Ratio", "11.718"},
            {"Tracking Error", "0.392"},
            {"Treynor Ratio", "-34.793"},
            {"Total Fees", "$19.35"},
            {"Estimated Strategy Capacity", "$100000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "190.82%"},
            {"OrderListHash", "f939eab32d8c58998c916d2153187537"}
        };
    }
}
