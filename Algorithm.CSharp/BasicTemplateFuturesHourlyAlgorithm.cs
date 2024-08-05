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
    /// This regressions tests the BasicTemplateFuturesDailyAlgorithm with hour data
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesHourlyAlgorithm : BasicTemplateFuturesDailyAlgorithm
    {
        protected override Resolution Resolution => Resolution.Hour;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 87289;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "716"},
            {"Average Win", "0.03%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "-1.716%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "-0.770"},
            {"Start Equity", "1000000"},
            {"End Equity", "982718.38"},
            {"Net Profit", "-1.728%"},
            {"Sharpe Ratio", "-8.845"},
            {"Sortino Ratio", "-5.449"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "96%"},
            {"Win Rate", "4%"},
            {"Profit-Loss Ratio", "4.89"},
            {"Alpha", "-0.018"},
            {"Beta", "-0.002"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.483"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "9.102"},
            {"Total Fees", "$1634.12"},
            {"Estimated Strategy Capacity", "$8000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "20.10%"},
            {"OrderListHash", "aa7e574f86b70428ca0afae381be80ba"}
        };
    }
}
