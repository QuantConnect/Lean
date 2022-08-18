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
        public override long DataPoints => 13571;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "53"},
            {"Average Win", "0.53%"},
            {"Average Loss", "-1.17%"},
            {"Compounding Annual Return", "-4.113%"},
            {"Drawdown", "8.000%"},
            {"Expectancy", "-0.709"},
            {"Net Profit", "-4.144%"},
            {"Sharpe Ratio", "-0.337"},
            {"Probabilistic Sharpe Ratio", "3.590%"},
            {"Loss Rate", "80%"},
            {"Win Rate", "20%"},
            {"Profit-Loss Ratio", "0.45"},
            {"Alpha", "-0.023"},
            {"Beta", "-0.022"},
            {"Annual Standard Deviation", "0.076"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-1.237"},
            {"Tracking Error", "0.119"},
            {"Treynor Ratio", "1.153"},
            {"Total Fees", "$110.61"},
            {"Estimated Strategy Capacity", "$620000.00"},
            {"Lowest Capacity Asset", "ES VP274HSU1AF5"},
            {"Fitness Score", "0.011"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-0.067"},
            {"Return Over Maximum Drawdown", "-0.515"},
            {"Portfolio Turnover", "0.024"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "ed88cc10fec4f936e1eb838f22983397"}
        };
    }
}
