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
*/

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template futures framework algorithm uses framework components to define an algorithm
    /// that trades futures.
    /// </summary>
    public class BasicTemplateFuturesFrameworkWithExtendedMarketAlgorithm : BasicTemplateFuturesFrameworkAlgorithm
    {
        protected override bool ExtendedMarketHours => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 70262;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-92.667%"},
            {"Drawdown", "5.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "96685.76"},
            {"Net Profit", "-3.314%"},
            {"Sharpe Ratio", "-6.359"},
            {"Sortino Ratio", "-11.237"},
            {"Probabilistic Sharpe Ratio", "9.333%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-1.47"},
            {"Beta", "0.312"},
            {"Annual Standard Deviation", "0.134"},
            {"Annual Variance", "0.018"},
            {"Information Ratio", "-14.77"},
            {"Tracking Error", "0.192"},
            {"Treynor Ratio", "-2.742"},
            {"Total Fees", "$4.62"},
            {"Estimated Strategy Capacity", "$52000000.00"},
            {"Lowest Capacity Asset", "GC VL5E74HP3EE5"},
            {"Portfolio Turnover", "43.77%"},
            {"OrderListHash", "dcdaafcefa47465962ace2759ed99c91"}
        };
    }
}
