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

        private readonly Queue<List<Symbol>> _expectedSymbols = new(new[]
        {
            new List<Symbol> { GetSymbol("SPY") },
            new List<Symbol> { GetSymbol("AAPL"), GetSymbol("IWM") },
            new List<Symbol> { GetSymbol("FB"), GetSymbol("AAPL"), GetSymbol("QQQ") },
        });

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 25);
            SetEndDate(2014, 03, 27);

            _universe = AddUniverse(SelectionFunction);
        }

        public IEnumerable<Symbol> SelectionFunction(IEnumerable<Fundamental> fundamentals)
        {
            var sortedByDollarVolume = fundamentals.OrderByDescending(x => x.DollarVolume);

            var top = sortedByDollarVolume.Skip(_selectionCount++).Take(_selectionCount).ToList();

            return top.Select(x => x.Symbol);
        }

        public override void OnData(Slice slice)
        {
            if (_universe.Selected.Contains(QuantConnect.Symbol.Create("TSLA", SecurityType.Equity, Market.USA)))
            {
                throw new Exception($"TSLA shouldn't of been selected");
            }

            if (Time.Date < new DateTime(2014, 03, 28))
            {
                var expectedSymbols = _expectedSymbols.Dequeue();

                if (!Enumerable.SequenceEqual(expectedSymbols, _universe.Selected))
                {
                    throw new Exception($"Unexpected selected symbols");
                }
            }

            Buy(_universe.Selected.First(), 1);
        }

        public override void OnEndOfAlgorithm()
        {
            if (_selectionCount != 3)
            {
                throw new Exception($"Unexpected selection count {_selectionCount}");
            }
            if (_universe.Selected.Count != 3 || _universe.Selected.Count == _universe.Members.Count)
            {
                throw new Exception($"Unexpected universe selected count {_universe.Selected.Count}");
            }
        }

        private static Symbol GetSymbol(string ticker) => QuantConnect.Symbol.Create(ticker, SecurityType.Equity, Market.USA);

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
        public long DataPoints => 28323;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-0.536%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99995.58"},
            {"Net Profit", "-0.004%"},
            {"Sharpe Ratio", "-70.905"},
            {"Sortino Ratio", "-70.905"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.01"},
            {"Beta", "0.003"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "12.072"},
            {"Tracking Error", "0.057"},
            {"Treynor Ratio", "-4.046"},
            {"Total Fees", "$3.00"},
            {"Estimated Strategy Capacity", "$680000000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.08%"},
            {"OrderListHash", "d80bfd86cd975ae3d29e174ec39a6e8e"}
        };
    }
}
