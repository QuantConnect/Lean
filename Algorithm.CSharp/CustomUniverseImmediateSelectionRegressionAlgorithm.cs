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
    /// Assert that custom universe selection happens right away after algorithm starts
    /// </summary>
    public class CustomUniverseImmediateSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly  List<Symbol> ExpectedSymbols = new List<Symbol>()
        {
            QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("APPL", SecurityType.Equity, Market.USA)
        };

        private bool _selected;
        private bool _securitiesChanged;

        private bool _firstOnData = true;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse(SecurityType.Equity,
                "my-custom-universe",
                Resolution.Daily,
                Market.USA,
                UniverseSettings,
                time =>
                {
                    _selected = true;
                    return new[] { "SPY", "GOOG", "APPL" };
                });
        }

        public override void OnData(Slice data)
        {
            if (_firstOnData)
            {
                if (!_selected)
                {
                    throw new Exception("Universe selection should have been triggered right away. " +
                        "The first OnData call should have had happened after the universe selection");
                }

                _firstOnData = false;
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (!_selected)
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

                if (changes.AddedSecurities.Count != ExpectedSymbols.Count)
                {
                    throw new Exception($"Expected {ExpectedSymbols.Count} stocks to be added to the algorithm, " +
                        $"but found {changes.AddedSecurities.Count}");
                }

                if (!ExpectedSymbols.All(x => changes.AddedSecurities.Any(security => security.Symbol == x)))
                {
                    throw new Exception("Expected symbols were not added to the algorithm");
                }

                _securitiesChanged = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_firstOnData || !_selected || !_securitiesChanged)
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
        public long DataPoints => 52;

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
            {"Information Ratio", "-8.91"},
            {"Tracking Error", "0.223"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
