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

using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that IsMarketOpen is working as expected
    /// </summary>
    public class IsMarketOpenCheckWithExtendedMarketHoursAlgorithm : IsMarketOpenCheckAlgorithm
    {
        protected override bool ExtendedMarketHours { get; } = true;

        protected override void ScheduleMarketOpenChecks()
        {
            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(3, 59, 59),
                () => AssertIsMarketOpen(expected: false));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(4, 0, 0),
                () => AssertIsMarketOpen(expected: true));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(9, 29, 59),
                () => AssertIsMarketOpen(expected: true));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(9, 30, 0),
                () => AssertIsMarketOpen(expected: true));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(15, 59, 59),
                () => AssertIsMarketOpen(expected: true));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(16, 0, 0),
                () => AssertIsMarketOpen(expected: true));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(19, 59, 59),
                () => AssertIsMarketOpen(expected: true));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(20, 0, 0),
                () => AssertIsMarketOpen(expected: false));

            Schedule.On(
                DateRules.EveryDay(Symbol),
                TimeRules.At(21, 0, 0),
                () => AssertIsMarketOpen(expected: false));

        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 9643;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
