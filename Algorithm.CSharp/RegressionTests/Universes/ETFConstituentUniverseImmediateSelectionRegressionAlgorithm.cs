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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Assert that ETF universe selection happens right away after algorithm starts
    /// </summary>
    public class ETFConstituentUniverseImmediateSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private List<Symbol> _constituents = new();

        private Symbol _spy;
        private bool _filtered;
        private bool _securitiesChanged;

        private bool _firstOnData = true;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 12, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Hour;

            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            AddUniverse(Universe.ETF(_spy, universeFilterFunc: FilterETFs));
        }

        /// <summary>
        /// Filters ETFs, performing some sanity checks
        /// </summary>
        /// <param name="constituents">Constituents of the ETF universe added above</param>
        /// <returns>Constituent Symbols to add to algorithm</returns>
        /// <exception cref="ArgumentException">Constituents collection was not structured as expected</exception>
        private IEnumerable<Symbol> FilterETFs(IEnumerable<ETFConstituentUniverse> constituents)
        {
            _filtered = true;
            _constituents = constituents.Select(x => x.Symbol).Distinct().ToList();

            return _constituents;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_firstOnData)
            {
                if (!_filtered)
                {
                    throw new Exception("Universe selection should have been triggered right away. " +
                        "The first OnData call should have had happened after the universe selection");
                }

                _firstOnData = false;
            }
        }

        /// <summary>
        /// Checks if new securities have been added to the algorithm after universe selection has occurred
        /// </summary>
        /// <param name="changes">Security changes</param>
        /// <exception cref="ArgumentException">Expected number of stocks were not added to the algorithm</exception>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (!_filtered)
            {
                throw new Exception("Universe selection should have been triggered right away");
            }

            if (!_securitiesChanged)
            {
                // Selection should be happening right on algorithm start
                if (Time != StartDate)
                {
                    throw new Exception("Universe selection should have been triggered right away");
                }

                // All constituents should have been added to the algorithm.
                // Plus the ETF itself.
                if (changes.AddedSecurities.Count != _constituents.Count + 1)
                {
                    throw new Exception($"Expected {_constituents.Count + 1} stocks to be added to the algorithm, " +
                        $"instead added: {changes.AddedSecurities.Count}");
                }

                if (!_constituents.All(constituent => changes.AddedSecurities.Any(security => security.Symbol == constituent)))
                {
                    throw new Exception("Not all constituents were added to the algorithm");
                }

                _securitiesChanged = true;
            }
        }

        /// <summary>
        /// Ensures that all expected events were triggered by the end of the algorithm
        /// </summary>
        /// <exception cref="Exception">An expected event didn't happen</exception>
        public override void OnEndOfAlgorithm()
        {
            if (_firstOnData || !_filtered || !_securitiesChanged)
            {
                throw new Exception("Expected events didn't happen");
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
        public long DataPoints => 2722;

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
            {"Information Ratio", "-0.695"},
            {"Tracking Error", "0.105"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
