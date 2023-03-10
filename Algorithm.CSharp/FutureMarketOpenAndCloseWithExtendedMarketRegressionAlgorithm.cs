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
using System.Linq;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to check we are getting the correct market open and close times when extended market hours are used
    /// </summary>
    public class FutureMarketOpenAndCloseWithExtendedMarketRegressionAlgorithm : FutureMarketOpenAndCloseRegressionAlgorithm
    {
        protected override bool ExtendedMarketHours => true;
        protected override List<DateTime> AfterMarketOpen => new List<DateTime>() {
            new DateTime(2022, 02, 01, 18, 0, 0), // Tuesday
            new DateTime(2022, 02, 02, 18, 0, 0),
            new DateTime(2022, 02, 03, 18, 0, 0),
            new DateTime(2022, 02, 06, 18, 0, 0),
            new DateTime(2022, 02, 07, 18, 0, 0),
            new DateTime(2022, 02, 08, 18, 0, 0)
        };
        protected override List<DateTime> BeforeMarketClose => new List<DateTime>()
        {
            new DateTime(2022, 02, 01, 17, 0, 0),
            new DateTime(2022, 02, 02, 17, 0, 0),
            new DateTime(2022, 02, 03, 17, 0, 0),
            new DateTime(2022, 02, 04, 17, 0, 0),
            new DateTime(2022, 02, 07, 17, 0, 0),
            new DateTime(2022, 02, 08, 17, 0, 0)
        };

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 82827;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
