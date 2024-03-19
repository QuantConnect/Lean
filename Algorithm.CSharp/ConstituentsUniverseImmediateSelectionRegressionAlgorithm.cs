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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Assert that constituents universe selection happens right away after algorithm starts
    /// </summary>
    public class ConstituentsUniverseImmediateSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly List<Symbol> _expectedConstituents = new()
        {
            QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
            QuantConnect.Symbol.Create("QQQ", SecurityType.Equity, Market.USA)
        };

        private bool _securitiesChanged;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 09);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;

            var customUniverseSymbol = new Symbol(
                SecurityIdentifier.GenerateConstituentIdentifier(
                    "constituents-universe-qctest",
                    SecurityType.Equity,
                    Market.USA),
                "constituents-universe-qctest");

            AddUniverse(new ConstituentsUniverse(customUniverseSymbol, UniverseSettings));
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (!_securitiesChanged)
            {
                // Selection should be happening right on algorithm start
                if (Time != StartDate)
                {
                    throw new Exception($"Universe selection should have been triggered right away on {StartDate} " +
                        $"but happened on {Time}");
                }

                // Constituents should have been added to the algorithm
                if (changes.AddedSecurities.Count != _expectedConstituents.Count)
                {
                    throw new Exception($"Expected {_expectedConstituents.Count} stocks to be added to the algorithm, " +
                        $"instead added: {changes.AddedSecurities.Count}");
                }

                if (!_expectedConstituents.All(constituent => changes.AddedSecurities.Any(security => security.Symbol == constituent)))
                {
                    throw new Exception("Not all constituents were added to the algorithm");
                }

                _securitiesChanged = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_securitiesChanged)
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
        public long DataPoints => 28;

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
