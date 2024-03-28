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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which reproduces GH issue 3740.
    /// We assert the methods are triggered at the correct algorithm time
    /// </summary>
    public class TimeRulesDefaultTimeZoneRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _scheduleEventEveryCallCount;
        private int _scheduleEventNoonCallCount;
        private int _scheduleEventMidnightCallCount;
        private int _selectionMethodCallCount;

        public override void Initialize()
        {
            SetStartDate(2017, 01, 01);
            SetEndDate(2017, 02, 01);

            SetUniverseSelection(new ScheduledUniverseSelectionModel(
                DateRules.EveryDay(),
                TimeRules.At(9, 31),
                SelectSymbolsAt
            ));

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromHours(6)), () =>
            {
                _scheduleEventEveryCallCount++;
                if (Time.Hour != 0
                    && Time.Hour != 6
                    && Time.Hour != 12
                    && Time.Hour != 18)
                {
                    throw new Exception($"Unexpected every 6 hours scheduled event time: {Time}");
                }
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.Noon, () =>
            {
                _scheduleEventNoonCallCount++;
                if (Time.Hour != 12)
                {
                    throw new Exception($"Unexpected Noon scheduled event time: {Time}");
                }
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.Midnight, () =>
            {
                _scheduleEventMidnightCallCount++;
                if (Time.Hour != 0)
                {
                    throw new Exception($"Unexpected Midnight scheduled event time: {Time}");
                }
            });
        }

        private IEnumerable<Symbol> SelectSymbolsAt(DateTime dateTime)
        {
            _selectionMethodCallCount++;
            Log($"SelectSymbolsAt {Time}");
            if (Time.TimeOfDay != new TimeSpan(9, 31, 0))
            {
                throw new Exception($"Expected 'SelectSymbolsAt' to be called at 9:31 algorithm time: {Time}");
            }
            yield break;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionMethodCallCount != 32)
            {
                throw new Exception($"Unexpected universe selection call count: {_selectionMethodCallCount}");
            }
            if (_scheduleEventEveryCallCount != 130)
            {
                throw new Exception($"Unexpected scheduled event call count: {_scheduleEventEveryCallCount}");
            }
            if (_scheduleEventNoonCallCount != 32)
            {
                throw new Exception($"Unexpected scheduled event call count: {_scheduleEventNoonCallCount}");
            }
            if (_scheduleEventMidnightCallCount != 33)
            {
                throw new Exception($"Unexpected scheduled event call count: {_scheduleEventMidnightCallCount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 187;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

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
            {"Information Ratio", "-2.962"},
            {"Tracking Error", "0.052"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
