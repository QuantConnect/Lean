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
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class ZeroDTEOptionsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<DateTime> _selectionDays;
        private int _currentSelectionDayIndex;

        private int _previouslyAddedContracts;

        public override void Initialize()
        {
            SetStartDate(2024, 01, 01);
            SetEndDate(2024, 01, 10);
            SetCash(100000);

            var equity = AddEquity("SPY");
            var option = AddOption(equity.Symbol);

            option.SetFilter(u => u.IncludeWeeklys().Expiration(0, 0));

            // use the underlying equity as the benchmark
            SetBenchmark(equity.Symbol);

            _selectionDays = new List<DateTime>()
            {
                new DateTime(2024, 01, 01), // Sunday midnight, already Monday 1st, it's a holiday. Selection happens for Tuesday here
                new DateTime(2024, 01, 03), // Wednesday, midnight
                new DateTime(2024, 01, 04),
                new DateTime(2024, 01, 05),
                new DateTime(2024, 01, 06), // Friday midnight, selection happens for Monday here
                new DateTime(2024, 01, 09), // Monday midnight, already Tuesday, selection happens for Tuesday here
                new DateTime(2024, 01, 10),
            };
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            // We expect selection every trading day
            if (Time.Date != _selectionDays[_currentSelectionDayIndex++])
            {
                throw new RegressionTestException($"Unexpected date. Expected {_selectionDays[_currentSelectionDayIndex]} but was {Time.Date}");
            }

            var addedOptions = changes.AddedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Option && !x.Symbol.IsCanonical()).ToList();

            if (addedOptions.Count == 0)
            {
                throw new RegressionTestException("No options were added");
            }

            var removedOptions = changes.RemovedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.Option && !x.Symbol.IsCanonical()).ToList();

            // Since we are selecting only 0DTE contracts, they must be deselected that same day
            if (removedOptions.Count != _previouslyAddedContracts)
            {
                throw new RegressionTestException($"Unexpected number of removed contracts. Expected {_previouslyAddedContracts} but was {removedOptions.Count}");
            }
            _previouslyAddedContracts = addedOptions.Count;
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
        public long DataPoints => 227;

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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
