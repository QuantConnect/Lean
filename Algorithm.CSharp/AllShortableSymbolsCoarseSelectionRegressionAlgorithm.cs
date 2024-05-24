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
using QuantConnect.Securities;
using QuantConnect.Data;
using QuantConnect.Data.Shortable;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using System.IO;

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

        private Security _security;

        public override void Initialize()
        {
            SetStartDate(2014, 3, 25);
            SetEndDate(2014, 3, 29);
            SetCash(10000000);
            _security = AddEquity(_spy);
            _security.SetShortableProvider(new RegressionTestShortableProvider());

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

            foreach (var (symbol, security) in ActiveSecurities.Where(kvp => !kvp.Value.Invested).OrderBy(kvp => kvp.Key))
            {
                var shortableQuantity = security.ShortableProvider.ShortableQuantity(symbol, Time);
                if (shortableQuantity == null)
                {
                    throw new Exception($"Expected {symbol} to be shortable on {Time:yyyy-MM-dd}");
                }

                // Buy at least once into all Symbols. Since daily data will always use
                // MOO orders, it makes the testing of liquidating buying into Symbols difficult.
                MarketOrder(symbol, -(decimal)shortableQuantity);
                _lastTradeDate = Time.Date;
            }
        }

        private IEnumerable<Symbol> CoarseSelection(IEnumerable<CoarseFundamental> coarse)
        {
            var shortableSymbols = (_security.ShortableProvider as dynamic).AllShortableSymbols(Time);
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
            }
            public override IShortableProvider GetShortableProvider(Security security)
            {
                return new RegressionTestShortableProvider();
            }
        }

        private class RegressionTestShortableProvider : LocalDiskShortableProvider
        {
            public RegressionTestShortableProvider() : base("testbrokerage")
            {
            }

            /// <summary>
            /// Gets a list of all shortable Symbols, including the quantity shortable as a Dictionary.
            /// </summary>
            /// <param name="localTime">The algorithm's local time</param>
            /// <returns>Symbol/quantity shortable as a Dictionary. Returns null if no entry data exists for this date or brokerage</returns>
            public Dictionary<Symbol, long> AllShortableSymbols(DateTime localTime)
            {
                var shortableDataDirectory = Path.Combine(Globals.DataFolder, SecurityType.Equity.SecurityTypeToLower(), Market.USA, "shortable", Brokerage);
                var allSymbols = new Dictionary<Symbol, long>();

                // Check backwards up to one week to see if we can source a previous file.
                // If not, then we return a list of all Symbols with quantity set to zero.
                var i = 0;
                while (i <= 7)
                {
                    var shortableListFile = Path.Combine(shortableDataDirectory, "dates", $"{localTime.AddDays(-i):yyyyMMdd}.csv");

                    foreach (var line in DataProvider.ReadLines(shortableListFile))
                    {
                        var csv = line.Split(',');
                        var ticker = csv[0];

                        var symbol = new Symbol(
                                SecurityIdentifier.GenerateEquity(ticker, QuantConnect.Market.USA,
                                    mappingResolveDate: localTime), ticker);
                        var quantity = Parse.Long(csv[1]);

                        allSymbols[symbol] = quantity;
                    }

                    if (allSymbols.Count > 0)
                    {
                        return allSymbols;
                    }

                    i++;
                }

                // Return our empty dictionary if we did not find a file to extract
                return allSymbols;
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
        public long DataPoints => 37748;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "19.147%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "10000000"},
            {"End Equity", "10019217.27"},
            {"Net Profit", "0.192%"},
            {"Sharpe Ratio", "15.743"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.17"},
            {"Beta", "0.037"},
            {"Annual Standard Deviation", "0.01"},
            {"Annual Variance", "0"},
            {"Information Ratio", "5"},
            {"Tracking Error", "0.094"},
            {"Treynor Ratio", "4.278"},
            {"Total Fees", "$307.50"},
            {"Estimated Strategy Capacity", "$2600000.00"},
            {"Lowest Capacity Asset", "GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "10.61%"},
            {"OrderListHash", "854d4ba6a4ae39f9be2f9a10c8544fe5"}
        };
    }
}
