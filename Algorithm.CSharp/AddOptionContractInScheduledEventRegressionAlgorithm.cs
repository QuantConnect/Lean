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

using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for GH issue #9279: adding a contract at/after its delisting date from a
    /// scheduled event used to emit a stale delisting warning right away, producing a spurious extra
    /// slice (and extra OnData call) at the same time. After the fix no delisting event is emitted for
    /// the late-added contract.
    /// </summary>
    public class AddOptionContractInScheduledEventRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contract;
        private bool _contractAdded;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 6);
            SetCash(100000);

            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            var aapl = AddEquity("AAPL", Resolution.Minute).Symbol;

            // Nearest-expiry contract (expires the same day): the case that used to trigger the stale warning
            _contract = OptionChain(aapl)
                .OrderBy(symbol => symbol.ID.Date)
                .ThenBy(symbol => symbol.ID.StrikePrice)
                .FirstOrDefault();

            Schedule.On(DateRules.On(2014, 6, 6), TimeRules.At(10, 0), () =>
            {
                if (_contract != null)
                {
                    AddOptionContract(_contract);
                    _contractAdded = true;
                }
            });
        }

        public override void OnData(Slice slice)
        {
            if (slice.Delistings.Count > 0)
            {
                throw new RegressionTestException(
                    $"Unexpected delisting event(s) at {Time} for a contract added at/after its delisting date " +
                    "inside a scheduled event (GH #9279).");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_contractAdded)
            {
                throw new RegressionTestException("Expected the option contract to be added during the scheduled event");
            }
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
        public long DataPoints => -1;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => -1;

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
