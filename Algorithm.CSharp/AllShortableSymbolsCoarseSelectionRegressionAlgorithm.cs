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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Shortable;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests filtering in coarse selection by shortable quantity
    /// </summary>
    public class AllShortableSymbolsCoarseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime _20140325 = new DateTime(2014, 3, 25);
        private static readonly DateTime _20140326 = new DateTime(2014, 3, 26);
        private static readonly DateTime _20140327 = new DateTime(2014, 3, 27);
        private static readonly DateTime _20140328 = new DateTime(2014, 3, 28);
        private static readonly DateTime _20140329 = new DateTime(2014, 3, 29);

        private static readonly Symbol _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        private static readonly Symbol _bac = QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA);
        private static readonly Symbol _gme = QuantConnect.Symbol.Create("GME", SecurityType.Equity, Market.USA);
        private static readonly Symbol _goog = QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA);
        private static readonly Symbol _qqq = QuantConnect.Symbol.Create("QQQ", SecurityType.Equity, Market.USA);
        private static readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private DateTime _lastTradeDate;

        private static readonly Dictionary<DateTime, bool> _coarseSelected = new Dictionary<DateTime, bool>
        {
            { _20140325, false },
            { _20140326, false },
            { _20140327, false },
            { _20140328, false },
        };

        private static readonly Dictionary<DateTime, Symbol[]> _expectedSymbols = new Dictionary<DateTime, Symbol[]>
        {
            { _20140325, new[]
                {
                    _bac,
                    _qqq,
                    _spy
                }
            },
            { _20140326, new[]
                {
                    _spy
                }
            },
            { _20140327, new[]
                {
                    _aapl,
                    _bac,
                    _gme,
                    _qqq,
                    _spy,
                }
            },
            { _20140328, new[]
                {
                    _goog
                }
            },
            { _20140329, new Symbol[0] }
        };

        public override void Initialize()
        {
            SetStartDate(2014, 3, 25);
            SetEndDate(2014, 3, 29);
            SetCash(10000000);

            AddUniverse(CoarseSelection);
            UniverseSettings.Resolution = Resolution.Daily;

            SetBrokerageModel(new AllShortableSymbolsRegressionAlgorithmBrokerageModel());
        }

        public override void OnData(Slice data)
        {
            if (Time.Date == _lastTradeDate)
            {
                return;
            }

            foreach (var symbol in ActiveSecurities.Keys)
            {
                if (!Portfolio.ContainsKey(symbol) || !Portfolio[symbol].Invested)
                {
                    if (!Shortable(symbol))
                    {
                        throw new Exception($"Expected {symbol} to be shortable on {Time:yyyy-MM-dd}");
                    }

                    // Buy at least once into all Symbols. Since daily data will always use
                    // MOO orders, it makes the testing of liquidating buying into Symbols difficult.
                    MarketOrder(symbol, -(decimal)ShortableQuantity(symbol));
                    _lastTradeDate = Time.Date;
                }
            }
        }

        private IEnumerable<Symbol> CoarseSelection(IEnumerable<CoarseFundamental> coarse)
        {
            var shortableSymbols = AllShortableSymbols();
            var selectedSymbols = coarse
                .Select(x => x.Symbol)
                .Where(s => shortableSymbols.ContainsKey(s) && shortableSymbols[s] >= 500)
                .OrderBy(s => s)
                .ToList();

            var expectedMissing = 0;
            if (Time.Date == _20140327)
            {
                var gme = QuantConnect.Symbol.Create("GME", SecurityType.Equity, Market.USA);
                if (!shortableSymbols.ContainsKey(gme))
                {
                    throw new Exception("Expected unmapped GME in shortable symbols list on 2014-03-27");
                }
                if (!coarse.Select(x => x.Symbol.Value).Contains("GME"))
                {
                    throw new Exception("Expected mapped GME in coarse symbols on 2014-03-27");
                }

                expectedMissing = 1;
            }

            var missing = _expectedSymbols[Time.Date].Except(selectedSymbols).ToList();
            if (missing.Count != expectedMissing)
            {
                throw new Exception($"Expected Symbols selected on {Time.Date:yyyy-MM-dd} to match expected Symbols, but the following Symbols were missing: {string.Join(", ", missing.Select(s => s.ToString()))}");
            }

            _coarseSelected[Time.Date] = true;
            return selectedSymbols;
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_coarseSelected.Values.All(x => x))
            {
                throw new AggregateException($"Expected coarse selection on all dates, but didn't run on: {string.Join(", ", _coarseSelected.Where(kvp => !kvp.Value).Select(kvp => kvp.Key.ToStringInvariant("yyyy-MM-dd")))}");
            }
        }

        private class AllShortableSymbolsRegressionAlgorithmBrokerageModel : DefaultBrokerageModel
        {
            public AllShortableSymbolsRegressionAlgorithmBrokerageModel() : base()
            {
                ShortableProvider = new RegressionTestShortableProvider();
            }
        }

        private class RegressionTestShortableProvider : LocalDiskShortableProvider
        {
            public RegressionTestShortableProvider() : base(SecurityType.Equity, "testbrokerage", Market.USA)
            {
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "36.294%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0.340%"},
            {"Sharpe Ratio", "21.2"},
            {"Probabilistic Sharpe Ratio", "99.990%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.274"},
            {"Beta", "0.138"},
            {"Annual Standard Deviation", "0.011"},
            {"Annual Variance", "0"},
            {"Information Ratio", "7.202"},
            {"Tracking Error", "0.068"},
            {"Treynor Ratio", "1.722"},
            {"Total Fees", "$307.50"},
            {"Fitness Score", "0.173"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0.173"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "6b1b205e5a6461ffd5bed645099714cd"}
        };
    }
}
