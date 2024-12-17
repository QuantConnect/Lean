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
        public override long DataPoints => 9788;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "34"},
            {"Average Win", "0.33%"},
            {"Average Loss", "-0.03%"},
            {"Compounding Annual Return", "0.124%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0.215"},
            {"Start Equity", "1000000"},
            {"End Equity", "1001253.7"},
            {"Net Profit", "0.125%"},
            {"Sharpe Ratio", "-1.663"},
            {"Sortino Ratio", "-0.78"},
            {"Probabilistic Sharpe Ratio", "15.471%"},
            {"Loss Rate", "88%"},
            {"Win Rate", "12%"},
            {"Profit-Loss Ratio", "9.33"},
            {"Alpha", "-0.007"},
            {"Beta", "0.002"},
            {"Annual Standard Deviation", "0.004"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.351"},
            {"Tracking Error", "0.089"},
            {"Treynor Ratio", "-4.101"},
            {"Total Fees", "$76.30"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "ES VRJST036ZY0X"},
            {"Portfolio Turnover", "0.92%"},
            {"OrderListHash", "bf28172e7f5f4b046454e6c79adb0809"}
        };
    }
}
