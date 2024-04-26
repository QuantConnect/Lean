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
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting consolidation happing flushed due to scan calls
    /// </summary>
    public class ConsolidateScanRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Queue<DateTime> _consolidationDaily = new();
        private readonly Queue<DateTime> _consolidationHourly = new();
        private readonly Queue<DateTime> _consolidation2Days = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 10);

            AddEquity("SPY", Resolution.Hour);
            Consolidate("SPY", Resolution.Daily, (TradeBar bar) =>
            {
                Debug($"Consolidated.Daily: {Time} {bar}");
                var expectedTime = _consolidationDaily.Dequeue();
                if (expectedTime != Time)
                {
                    throw new Exception($"Unexpected consolidation time {expectedTime} != {Time}");
                }

                if (!Portfolio.Invested)
                {
                    SetHoldings("SPY", 1);
                }
            });
            _consolidationDaily.Enqueue(new DateTime(2013, 10, 8, 0, 0, 0));
            _consolidationDaily.Enqueue(new DateTime(2013, 10, 9, 0, 0, 0));
            _consolidationDaily.Enqueue(new DateTime(2013, 10, 10, 0, 0, 0));

            Consolidate("SPY", TimeSpan.FromHours(3), (TradeBar bar) =>
            {
                Debug($"Consolidated.FromHours(3): {Time} {bar}");
                var expectedTime = _consolidationHourly.Dequeue();
                if (expectedTime != Time)
                {
                    throw new Exception($"Unexpected consolidation time {expectedTime} != {Time} 3 hours");
                }
            });
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 7, 12, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 7, 15, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 7, 18, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 8, 12, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 8, 15, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 8, 18, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 9, 12, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 9, 15, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 9, 18, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 10, 12, 0, 0));
            _consolidationHourly.Enqueue(new DateTime(2013, 10, 10, 15, 0, 0));

            Consolidate("SPY", TimeSpan.FromDays(2), (TradeBar bar) =>
            {
                Debug($"Consolidated.2Days: {Time} {bar}");
                var expectedTime = _consolidation2Days.Dequeue();
                if (expectedTime != Time)
                {
                    throw new Exception($"Unexpected consolidation time {expectedTime} != {Time} 2 days");
                }
            });
            _consolidation2Days.Enqueue(new DateTime(2013, 10, 9, 9, 0, 0));
        }

        public override void OnEndOfAlgorithm()
        {
            if (_consolidationDaily.Count != 0 || _consolidationHourly.Count != 0 || _consolidation2Days.Count != 0)
            {
                throw new Exception($"Unexpected consolidation count");
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
        public long DataPoints => 64;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "186.478%"},
            {"Drawdown", "1.500%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101062.91"},
            {"Net Profit", "1.063%"},
            {"Sharpe Ratio", "5.448"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.055"},
            {"Beta", "1.003"},
            {"Annual Standard Deviation", "0.272"},
            {"Annual Variance", "0.074"},
            {"Information Ratio", "-33.89"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.479"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$130000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "25.24%"},
            {"OrderListHash", "faeb006f6e2015131523994ae78d4eb7"}
        };
    }
}
