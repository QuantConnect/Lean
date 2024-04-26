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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regressions tests the BasicTemplateFuturesDailyAlgorithm with hour data and extended market hours
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesWithExtendedMarketHourlyAlgorithm : BasicTemplateFuturesHourlyAlgorithm
    {
        protected override bool ExtendedMarketHours => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 228938;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1990"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-4.683%"},
            {"Drawdown", "4.700%"},
            {"Expectancy", "-0.911"},
            {"Start Equity", "1000000"},
            {"End Equity", "952831.02"},
            {"Net Profit", "-4.717%"},
            {"Sharpe Ratio", "-7.178"},
            {"Sortino Ratio", "-5.126"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "97%"},
            {"Win Rate", "3%"},
            {"Profit-Loss Ratio", "2.04"},
            {"Alpha", "-0.038"},
            {"Beta", "-0.008"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.702"},
            {"Tracking Error", "0.09"},
            {"Treynor Ratio", "5.049"},
            {"Total Fees", "$4538.98"},
            {"Estimated Strategy Capacity", "$3000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "56.68%"},
            {"OrderListHash", "60f85901ecc345e597c0153506792285"}
        };
    }
}
