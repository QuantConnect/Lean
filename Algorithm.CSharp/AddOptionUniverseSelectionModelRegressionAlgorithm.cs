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
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Interfaces;
using System;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to use the OptionUniverseSelectionModel to select options contracts based on specified conditions.
    /// The model is updated daily and selects different options based on the current date.
    /// The algorithm ensures that only valid option contracts are selected for the universe.
    /// </summary>
    public class AddOptionUniverseSelectionModelRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _optionCount;
        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 06);

            UniverseSettings.Resolution = Resolution.Minute;
            SetUniverseSelection(new OptionUniverseSelectionModel(
                TimeSpan.FromDays(1),
                SelectOptionChainSymbols
            ));
        }

        private static IEnumerable<Symbol> SelectOptionChainSymbols(DateTime utcTime)
        {
            var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
            if (newYorkTime.Date < new DateTime(2014, 06, 06))
            {
                yield return QuantConnect.Symbol.Create("TWX", SecurityType.Option, Market.USA);
            }

            if (newYorkTime.Date >= new DateTime(2014, 06, 06))
            {
                yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Option, Market.USA);
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (changes.AddedSecurities.Count > 0)
            {
                foreach (var security in changes.AddedSecurities)
                {
                    var symbol = security.Symbol.Underlying == null ? security.Symbol : security.Symbol.Underlying;
                    if (symbol != "AAPL" && symbol != "TWX")
                    {
                        throw new RegressionTestException($"Unexpected security {security.Symbol}");
                    }
                    _optionCount += (security.Symbol.SecurityType == SecurityType.Option) ? 1 : 0;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (ActiveSecurities.Count == 0)
            {
                throw new RegressionTestException("No active securities found. Expected at least one active security");
            }
            if (_optionCount == 0)
            {
                throw new RegressionTestException("The option count should be greater than 0");
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
        public long DataPoints => 1658167;

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
