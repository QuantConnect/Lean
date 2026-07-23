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
        public override List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 68170;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "170"},
            {"Average Win", "0.03%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-0.171%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-0.100"},
            {"Start Equity", "1000000"},
            {"End Equity", "998281.54"},
            {"Net Profit", "-0.172%"},
            {"Sharpe Ratio", "-1.251"},
            {"Sortino Ratio", "-0.548"},
            {"Probabilistic Sharpe Ratio", "2.934%"},
            {"Loss Rate", "65%"},
            {"Win Rate", "35%"},
            {"Profit-Loss Ratio", "1.61"},
            {"Alpha", "-0.007"},
            {"Beta", "-0.005"},
            {"Annual Standard Deviation", "0.006"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.354"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "1.669"},
            {"Total Fees", "$383.46"},
            {"Estimated Strategy Capacity", "$4800000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "4.89%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "fce6e574beb20c50ccfb1191dfade7f2"}
        };
    }
}
