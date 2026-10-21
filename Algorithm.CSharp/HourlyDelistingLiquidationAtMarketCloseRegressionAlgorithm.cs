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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that an equity with hourly data held through its delisting is liquidated with a
    /// market on close order, submitted the hour before the close, at the close of its last trading day
    /// </summary>
    public class HourlyDelistingLiquidationAtMarketCloseRegressionAlgorithm : DelistingLiquidationAtMarketCloseRegressionAlgorithm
    {
        protected override string Ticker => "GDVD";

        protected override Resolution Resolution => Resolution.Hour;

        protected override DateTime ExpectedLiquidationTime => new DateTime(2021, 1, 20, 16, 0, 0);

        protected override DateTime Start => new DateTime(2021, 1, 11);

        protected override DateTime End => new DateTime(2021, 1, 25);

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 130;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.27%"},
            {"Compounding Annual Return", "-6.437%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "99733.01"},
            {"Net Profit", "-0.267%"},
            {"Sharpe Ratio", "-2.564"},
            {"Sortino Ratio", "-2.492"},
            {"Probabilistic Sharpe Ratio", "19.701%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.09"},
            {"Beta", "0.037"},
            {"Annual Standard Deviation", "0.031"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-4.64"},
            {"Tracking Error", "0.084"},
            {"Treynor Ratio", "-2.143"},
            {"Total Fees", "$15.55"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "GDVD WKELFN09PUN9"},
            {"Portfolio Turnover", "6.63%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "228488506eb6c3b122f0cd53dc081b0c"}
        };
    }
}
