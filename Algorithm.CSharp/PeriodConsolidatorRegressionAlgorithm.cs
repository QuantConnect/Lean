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
    /// Regression algorithm asserting the behavior of a period consolidator
    /// </summary>
    public class PeriodConsolidatorRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Queue<string> _periodConsolidation = new();
        private Queue<string> _countConsolidation = new();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 08);
            
            var symbol = AddEquity("SPY").Symbol;

            var periodConsolidator = new TradeBarConsolidator(Resolution.Minute.ToTimeSpan());
            periodConsolidator.DataConsolidated += PeriodConsolidator_DataConsolidated;
            var countConsolidator = new TradeBarConsolidator(1);
            countConsolidator.DataConsolidated += CountConsolidator_DataConsolidated;

            SubscriptionManager.AddConsolidator(symbol, periodConsolidator);
            SubscriptionManager.AddConsolidator(symbol, countConsolidator);
        }

        private void PeriodConsolidator_DataConsolidated(object sender, TradeBar e)
        {
            _periodConsolidation.Enqueue($"{Time} - {e.EndTime} {e}");
        }
        private void CountConsolidator_DataConsolidated(object sender, TradeBar e)
        {
            _countConsolidation.Enqueue($"{Time} - {e.EndTime} {e}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (_countConsolidation.Count == 0 || _countConsolidation.Count != _periodConsolidation.Count)
            {
                throw new RegressionTestException($"Unexpected consolidated data count. Period: {_periodConsolidation.Count} Count: {_countConsolidation.Count}");
            }

            while (_countConsolidation.TryDequeue(out var countData))
            {
                var periodData = _periodConsolidation.Dequeue();
                if (periodData != countData)
                {
                    throw new RegressionTestException($"Unexpected consolidated data. Period: '{periodData}' != Count: '{countData}'");
                }
            }
            _periodConsolidation.Clear();
            _countConsolidation.Clear();
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1582;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 10;

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
