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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that a future added with a data mapping mode its market has no data for (open interest on EUREX)
    /// falls back to the market default, and that the related warnings are sent.
    /// </summary>
    public class FutureDataMappingModeFallbackRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _future;
        private bool _checkedAfterInitialize;

        public override void Initialize()
        {
            SetStartDate(2024, 6, 3);
            SetEndDate(2024, 6, 4);
            SetAccountCurrency(Currencies.EUR);

            _future = AddFuture(Futures.Indices.EuroStoxx50, Resolution.Minute, dataMappingMode: DataMappingMode.OpenInterest);
            AssertWarning("Warning: OpenInterest data mapping mode is not available for EUREX futures, using LastTradingDay instead.");

            // Open interest resolves to the mode already in use, so this is not a conflicting re-add
            AddFuture(Futures.Indices.EuroStoxx50, Resolution.Minute, dataMappingMode: DataMappingMode.LastTradingDay);
            if (DebugMessages.Any(message => message.Contains("was already added")))
            {
                throw new RegressionTestException("Unexpected re-add warning for a future added again with the same data mapping mode");
            }
        }

        public override void OnData(Slice slice)
        {
            if (_checkedAfterInitialize || _future.Mapped == null)
            {
                return;
            }
            _checkedAfterInitialize = true;

            // Last trading day maps to the June contract, first day of the month would map to September
            if (_future.Mapped.ID.Date.Month != 6)
            {
                throw new RegressionTestException($"Unexpected mapped contract {_future.Mapped}, expected the June contract");
            }

            if (History(_future.Symbol, 10, Resolution.Minute).Count() == 0)
            {
                throw new RegressionTestException("Expected history for the continuous future using the fallback data mapping mode");
            }

            var openInterestHistory = History(_future.Symbol, 10, Resolution.Minute, dataMappingMode: DataMappingMode.OpenInterest);
            if (openInterestHistory.Any())
            {
                throw new RegressionTestException("Expected no history for the explicitly requested open interest data mapping mode");
            }
            AssertWarning("Warning: OpenInterest data mapping mode is not available for EUREX futures, no contract will be mapped. Use LastTradingDay instead.");

            AddFuture(Futures.Indices.EuroStoxx50, Resolution.Minute, dataMappingMode: DataMappingMode.FirstDayMonth);
            AssertWarning("Warning: /FESX was already added, ignoring the requested data mapping mode FirstDayMonth (keeping LastTradingDay). " +
                "To change these settings, remove it with RemoveSecurity() and add it again.");
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_checkedAfterInitialize)
            {
                throw new RegressionTestException("The continuous future was never mapped");
            }
            if (_future.Mapped.ID.Date.Month != 6)
            {
                throw new RegressionTestException($"Unexpected mapped contract {_future.Mapped} after the ignored re-add, expected the June contract");
            }
        }

        private void AssertWarning(string warning)
        {
            if (DebugMessages.Count(message => message.EndsWith(warning)) != 1)
            {
                throw new RegressionTestException($"Expected the warning '{warning}' to be sent once");
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
        public long DataPoints => 5010;

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
            {"Total Fees", "€0.00"},
            {"Estimated Strategy Capacity", "€0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"},
        };
    }
}
