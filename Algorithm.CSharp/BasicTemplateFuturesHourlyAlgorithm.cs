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
        public override long DataPoints => 25409;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "66"},
            {"Average Win", "0.07%"},
            {"Average Loss", "-0.04%"},
            {"Compounding Annual Return", "0.296%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0.213"},
            {"Start Equity", "1000000"},
            {"End Equity", "1002984.28"},
            {"Net Profit", "0.298%"},
            {"Sharpe Ratio", "-0.872"},
            {"Sortino Ratio", "-0.342"},
            {"Probabilistic Sharpe Ratio", "6.244%"},
            {"Loss Rate", "53%"},
            {"Win Rate", "47%"},
            {"Profit-Loss Ratio", "1.59"},
            {"Alpha", "-0.005"},
            {"Beta", "-0.001"},
            {"Annual Standard Deviation", "0.005"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.325"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "4.124"},
            {"Total Fees", "$143.22"},
            {"Estimated Strategy Capacity", "$13000000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Portfolio Turnover", "1.86%"},
            {"Drawdown Recovery", "165"},
            {"OrderListHash", "12f89a137598802c39e71ee4bfdb522b"}
        };
    }
}
