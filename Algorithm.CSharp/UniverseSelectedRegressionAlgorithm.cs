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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of Universe.Selected collection
    /// </summary>
    public class UniverseSelectedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _selectionCount;
        private Universe _universe;

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 03, 28);

            _universe = AddUniverse(SelectionFunction);
        }

        public IEnumerable<Symbol> SelectionFunction(IEnumerable<Fundamental> fundamentals)
        {
            var sortedByDollarVolume = fundamentals.OrderByDescending(x => x.DollarVolume);

            var top = sortedByDollarVolume.Skip(_selectionCount++).Take(1).ToList();

            return top.Select(x => x.Symbol);
        }

        public override void OnData(Slice slice)
        {
            if (_universe.Selected.Contains(QuantConnect.Symbol.Create("TSLA", SecurityType.Equity, Market.USA)))
            {
                throw new Exception($"Unexpected selected symbol");
            }
            Buy(_universe.Selected.Single(), 1);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionCount != 5)
            {
                throw new Exception($"Unexpected selection count {_selectionCount}");
            }
            if (_universe.Selected.Count != 1 || _universe.Selected.Count == _universe.Members.Count)
            {
                throw new Exception($"Unexpected universe selected count {_universe.Selected.Count}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 35405;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.665%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99990.86"},
            {"Net Profit", "-0.009%"},
            {"Sharpe Ratio", "-34.742"},
            {"Sortino Ratio", "-34.742"},
            {"Probabilistic Sharpe Ratio", "0.000%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.013"},
            {"Beta", "0.004"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.467"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "-3.739"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$880000000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.05%"},
            {"OrderListHash", "8718aa59db1c32917e24e902ca43cb64"}
        };
    }
}
