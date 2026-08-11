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
    /// Regression algorithm asserting the scheduling convenience APIs: an every-N-days date rule,
    /// a time rule with an IANA time zone id and scheduling with a default midnight time rule
    /// </summary>
    public class SchedulingConvenienceRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly List<DateTime> _everyThreeDaysTimes = new();
        private readonly List<DateTime> _defaultTimeRuleTimes = new();
        private readonly List<DateTime> _londonTimes = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            // every 3 days, anchored at the start of the schedule
            Schedule.On(DateRules.Every(TimeSpan.FromDays(3)), TimeRules.At(12, 0), () => _everyThreeDaysTimes.Add(Time));

            // no time rule: defaults to midnight in the algorithm time zone
            Schedule.On(DateRules.EveryDay(), () => _defaultTimeRuleTimes.Add(Time));

            // IANA time zone id: 15:00 London (BST in October 2013) is 10:00 New York (EDT)
            Schedule.On(DateRules.EveryDay(), TimeRules.At(15, 0, "Europe/London"), () => _londonTimes.Add(Time));
        }

        public override void OnEndOfAlgorithm()
        {
            // the schedule starts the day before the algorithm start, so the rule anchors at Oct 6 and
            // fires Oct 6 (before the start, skipped), Oct 9 and Oct 12 (after the end)
            AssertScheduledTimes(_everyThreeDaysTimes, new List<DateTime> { new(2013, 10, 09, 12, 0, 0) }, "every 3 days");

            // fires at midnight every day, including the algorithm start time itself
            AssertScheduledTimes(_defaultTimeRuleTimes,
                Enumerable.Range(7, 5).Select(day => new DateTime(2013, 10, day)).ToList(), "default midnight");

            AssertScheduledTimes(_londonTimes,
                Enumerable.Range(7, 5).Select(day => new DateTime(2013, 10, day, 10, 0, 0)).ToList(), "London 15:00");
        }

        private static void AssertScheduledTimes(List<DateTime> actual, List<DateTime> expected, string name)
        {
            if (!actual.SequenceEqual(expected))
            {
                throw new RegressionTestException($"Unexpected '{name}' event times: expected [{string.Join(", ", expected)}] " +
                    $"but got [{string.Join(", ", actual)}]");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 42;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
