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
using QuantConnect.Data.Fundamental;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that multiple universe selection functions are called
    /// in the order the universes were added to the algorithm
    /// </summary>
    public class MultipleUniverseSelectionOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _selectionCallCount;

        public override void Initialize()
        {
            SetStartDate(2014, 3, 24);
            SetEndDate(2014, 3, 28);
            UniverseSettings.Resolution = Resolution.Daily;

            AddUniverse(SelectAssets1);
            AddUniverse(SelectAssets2);
            AddUniverse(SelectAssets3);
        }

        private IEnumerable<Symbol> SelectAssets1(IEnumerable<Fundamental> fundamentals)
        {
            ValidateSelectionOrder(1);
            return Enumerable.Empty<Symbol>();
        }

        private IEnumerable<Symbol> SelectAssets2(IEnumerable<Fundamental> fundamentals)
        {
            ValidateSelectionOrder(2);
            return Enumerable.Empty<Symbol>();
        }

        private IEnumerable<Symbol> SelectAssets3(IEnumerable<Fundamental> fundamentals)
        {
            ValidateSelectionOrder(3);
            return Enumerable.Empty<Symbol>();
        }

        private void ValidateSelectionOrder(int universeIndex)
        {
            var expectedPositionInCycle = universeIndex - 1;
            if (_selectionCallCount % 3 != expectedPositionInCycle)
            {
                throw new RegressionTestException($"Universes are not being selected in the order they were added. Expected universe {expectedPositionInCycle + 1} but got universe {universeIndex}.");
            }
            _selectionCallCount++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionCallCount < 3)
            {
                throw new RegressionTestException($"Expected all 3 universes to be selected at least once, but got {_selectionCallCount} calls.");
            }
        }

        public bool CanRunLocally { get; } = true;

        public List<Language> Languages { get; } = new() { Language.CSharp };

        public long DataPoints => -1;

        public int AlgorithmHistoryDataPoints => 0;

        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

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
            {"Information Ratio", "-0.404"},
            {"Tracking Error", "0.094"},
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
