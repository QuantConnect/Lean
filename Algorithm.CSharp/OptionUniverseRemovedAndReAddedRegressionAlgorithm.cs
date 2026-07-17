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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that removing an option universe and re-adding one for the
    /// same underlying within the same time step does not throw, and that the re-added universe
    /// keeps providing option chain data.
    /// </summary>
    public class OptionUniverseRemovedAndReAddedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _canonical;
        private bool _readded;
        private int _chainsBefore;
        private int _chainsAfter;

        public override void Initialize()
        {
            SetStartDate(2014, 6, 6);
            SetEndDate(2014, 6, 9);
            SetCash(100000);

            _canonical = AddAaplOption();
        }

        private Symbol AddAaplOption()
        {
            var option = AddOption("AAPL", Resolution.Minute);
            option.SetFilter(-2, 2, 0, 180);
            return option.Symbol;
        }

        public override void OnData(Slice slice)
        {
            var hasChain = slice.OptionChains.TryGetValue(_canonical, out var chain) && chain.Any();

            if (!_readded)
            {
                if (hasChain)
                {
                    _chainsBefore++;
                }

                if (Time.Hour >= 10)
                {
                    // Remove the option universe and re-add one for the same underlying in the same time step
                    RemoveSecurity(_canonical);
                    _canonical = AddAaplOption();
                    _readded = true;
                }
            }
            else if (hasChain)
            {
                _chainsAfter++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_readded)
            {
                throw new RegressionTestException("The option universe was never removed and re-added");
            }
            if (_chainsBefore == 0)
            {
                throw new RegressionTestException("Expected option chain data before the universe was removed");
            }
            if (_chainsAfter == 0)
            {
                throw new RegressionTestException("Expected option chain data after the universe was re-added");
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
        public long DataPoints => 108983;

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
            {"Information Ratio", "-9.486"},
            {"Tracking Error", "0.008"},
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
