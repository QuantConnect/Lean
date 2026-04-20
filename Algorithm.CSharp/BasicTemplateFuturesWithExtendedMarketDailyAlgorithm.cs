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
        public override long DataPoints => 5971;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "36"},
            {"Average Win", "0.33%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "0.103%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0.172"},
            {"Start Equity", "1000000"},
            {"End Equity", "1001033.76"},
            {"Net Profit", "0.103%"},
            {"Sharpe Ratio", "-1.701"},
            {"Sortino Ratio", "-0.809"},
            {"Probabilistic Sharpe Ratio", "14.685%"},
            {"Loss Rate", "89%"},
            {"Win Rate", "11%"},
            {"Profit-Loss Ratio", "9.55"},
            {"Alpha", "-0.007"},
            {"Beta", "0.002"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.353"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "-4.042"},
            {"Total Fees", "$81.24"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Portfolio Turnover", "0.99%"},
            {"Drawdown Recovery", "69"},
            {"OrderListHash", "67120ad5c9a6116001dda6c8061e5353"}
        };
    }
}
