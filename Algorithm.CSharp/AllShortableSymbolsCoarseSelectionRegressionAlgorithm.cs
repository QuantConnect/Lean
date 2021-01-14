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
    public class AllShortableSymbolsCoarseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime _20140325 = new DateTime(2014, 3, 25);
        private static readonly DateTime _20140326 = new DateTime(2014, 3, 26);
        private static readonly DateTime _20140327 = new DateTime(2014, 3, 27);
        private static readonly DateTime _20140328 = new DateTime(2014, 3, 28);
        private static readonly Symbol _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        private static readonly Symbol _bac = QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA);
        private static readonly Symbol _gme = QuantConnect.Symbol.Create("GME", SecurityType.Equity, Market.USA);
        private static readonly Symbol _goog = QuantConnect.Symbol.Create("GOOG", SecurityType.Equity, Market.USA);
        private static readonly Symbol _jpm = QuantConnect.Symbol.Create("JPM", SecurityType.Equity, Market.USA);
        private static readonly Symbol _qqq = QuantConnect.Symbol.Create("QQQ", SecurityType.Equity, Market.USA);
        private static readonly Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private static readonly Symbol _wtw = QuantConnect.Symbol.Create("WTW", SecurityType.Equity, Market.USA);

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
                    _goog,
                    _qqq,
                    _spy,
                }
            },
            { _20140328, new Symbol[0] }
        };

        public override void Initialize()
        {
            SetStartDate(2014, 3, 25);
            SetEndDate(2014, 3, 28);
            SetCash(100000);

            AddUniverse(CoarseSelection);
            SetBrokerageModel(new AllShortableSymbolsRegressionAlgorithmBrokerageModel());
        }

        public override void OnData(Slice data)
        {
        }

        private IEnumerable<Symbol> CoarseSelection(IEnumerable<CoarseFundamental> coarse)
        {
            Log($"Coarse selection at {Time:yyyy-MM-dd}");

            var shortableSymbols = AllShortableSymbols();
            var selectedSymbols = coarse
                .Select(x => x.Symbol)
                .Where(s => shortableSymbols.ContainsKey(s) && shortableSymbols[s] >= 500)
                .OrderBy(s => s)
                .ToList();

            if (_expectedSymbols[Time.Date].Except(selectedSymbols).Count() != 0)
            {
                throw new Exception($"Expected Symbols selected on {Time.Date:yyyy-MM-dd} to match expected Symbols, but the following Symbols were missing: {string.Join(", ", _expectedSymbols[Time.Date].Except(selectedSymbols).Select(s => s.ToString()))}");
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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics { get; }
    }
}
