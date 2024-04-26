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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add futures with daily resolution and extended market hours.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesWithExtendedMarketDailyAlgorithm : BasicTemplateFuturesDailyAlgorithm
    {
        protected override bool ExtendedMarketHours => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 16265;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "156"},
            {"Average Win", "0.31%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.024%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "-0.035"},
            {"Start Equity", "1000000"},
            {"End Equity", "999754.94"},
            {"Net Profit", "-0.025%"},
            {"Sharpe Ratio", "-1.602"},
            {"Sortino Ratio", "-1.913"},
            {"Probabilistic Sharpe Ratio", "11.172%"},
            {"Loss Rate", "97%"},
            {"Win Rate", "3%"},
            {"Profit-Loss Ratio", "36.65"},
            {"Alpha", "-0.007"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.359"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "8.008"},
            {"Total Fees", "$347.56"},
            {"Estimated Strategy Capacity", "$1000.00"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Portfolio Turnover", "4.16%"},
            {"OrderListHash", "52580f1e94ab1875301d3bbd157f4580"}
        };
    }
}
