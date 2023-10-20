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
        public override long DataPoints => 16263;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "154"},
            {"Average Win", "0.09%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-0.648%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "-0.872"},
            {"Net Profit", "-0.653%"},
            {"Sharpe Ratio", "-6.358"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "99%"},
            {"Win Rate", "1%"},
            {"Profit-Loss Ratio", "8.82"},
            {"Alpha", "-0.012"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.409"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "9.551"},
            {"Total Fees", "$343.26"},
            {"Estimated Strategy Capacity", "$1000.00"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Portfolio Turnover", "4.12%"},
            {"OrderListHash", "d3df9cf10bcbf04063b861576bd4fed1"}
        };
    }
}
