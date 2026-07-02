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
        public override List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 5980;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "22"},
            {"Average Win", "0.24%"},
            {"Average Loss", "-0.49%"},
            {"Compounding Annual Return", "-0.252%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "-0.258"},
            {"Start Equity", "1000000"},
            {"End Equity", "997465.73"},
            {"Net Profit", "-0.253%"},
            {"Sharpe Ratio", "-5.753"},
            {"Sortino Ratio", "-1.032"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "0.48"},
            {"Alpha", "-0.009"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0.002"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.381"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "-19.581"},
            {"Total Fees", "$6.77"},
            {"Estimated Strategy Capacity", "$290000000.00"},
            {"Lowest Capacity Asset", "GC VOFJUCDY9XNH"},
            {"Portfolio Turnover", "0.12%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "140ff4560d532192be3041846667deca"}
        };
    }
}
