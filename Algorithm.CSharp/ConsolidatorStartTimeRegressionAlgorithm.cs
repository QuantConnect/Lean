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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm show casing and asserting the behavior of creating a consolidator specifying the start time
    /// </summary>
    public class ConsolidatorStartTimeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Queue<TimeSpan> _expectedConsolidationTime = new([
            new TimeSpan(9, 30, 0),
            new TimeSpan(10, 30, 0),
            new TimeSpan(11, 30, 0),
            new TimeSpan(12, 30, 0),
            new TimeSpan(13, 30, 0),
            new TimeSpan(14, 30, 0)
        ]);
        private TradeBarConsolidator consolidator;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 04);
            SetEndDate(2013, 10, 04);

            AddEquity("SPY", Resolution.Minute);

            consolidator = new TradeBarConsolidator(TimeSpan.FromHours(1), startTime: new TimeSpan(9, 30, 0));
            consolidator.DataConsolidated += BarHandler;

            SubscriptionManager.AddConsolidator("SPY", consolidator);
        }

        private void BarHandler(object _, TradeBar bar)
        {
            if (Time != bar.EndTime)
            {
                throw new RegressionTestException($"Unexpected consolidation time {bar.Time} != {Time}!");
            }

            var expected = _expectedConsolidationTime.Dequeue();
            if (bar.Time.TimeOfDay != expected)
            {
                throw new RegressionTestException($"Unexpected consolidation time {bar.Time.TimeOfDay} != {expected}!");
            }

            if (bar.Period != TimeSpan.FromHours(1))
            {
                throw new RegressionTestException($"Unexpected consolidation period {bar.Period}!");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_expectedConsolidationTime.Count > 0)
            {
                throw new RegressionTestException("Unexpected consolidation times!");
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
        public long DataPoints => 795;

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
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
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
