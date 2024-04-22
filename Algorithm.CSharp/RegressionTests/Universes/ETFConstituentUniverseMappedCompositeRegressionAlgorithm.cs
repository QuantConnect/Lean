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
    /// Tests the mapping of the ETF symbol that has a constituent universe attached to it and ensures
    /// that data is loaded after the mapping event takes place.
    /// </summary>
    public class ETFConstituentUniverseMappedCompositeRegressionAlgorithm: QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;
        private Symbol _qqq;
        private Dictionary<DateTime, int> _filterDateConstituentSymbolCount = new Dictionary<DateTime, int>();
        private Dictionary<DateTime, bool> _constituentDataEncountered = new Dictionary<DateTime, bool>();
        private HashSet<Symbol> _constituentSymbols = new HashSet<Symbol>();
        private bool _mappingEventOccurred;
        
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2011, 2, 1);
            SetEndDate(2011, 4, 4);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Hour;

            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            _qqq = AddEquity("QQQ", Resolution.Daily).Symbol;
            AddUniverse(Universe.ETF(_qqq, universeFilterFunc: FilterETFs));
        }

        private IEnumerable<Symbol> FilterETFs(IEnumerable<ETFConstituentUniverse> constituents)
        {
            var constituentSymbols = constituents.Select(x => x.Symbol).ToHashSet();
            if (!constituentSymbols.Contains(_aapl))
            {
                throw new Exception("AAPL not found in QQQ constituents");
            }
            
            _filterDateConstituentSymbolCount[UtcTime.Date] = constituentSymbols.Count;
            foreach (var symbol in constituentSymbols)
            {
                _constituentSymbols.Add(symbol);
            }
            
            return constituentSymbols;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (data.SymbolChangedEvents.Count != 0)
            {
                foreach (var symbolChanged in data.SymbolChangedEvents.Values)
                {
                    if (symbolChanged.Symbol != _qqq)
                    {
                        throw new Exception($"Mapped symbol is not QQQ. Instead, found: {symbolChanged.Symbol}");
                    }
                    if (symbolChanged.OldSymbol != "QQQQ")
                    {
                        throw new Exception($"Old QQQ Symbol is not QQQQ. Instead, found: {symbolChanged.OldSymbol}");
                    }
                    if (symbolChanged.NewSymbol != "QQQ")
                    {
                        throw new Exception($"New QQQ Symbol is not QQQ. Instead, found: {symbolChanged.NewSymbol}");
                    }
                    
                    _mappingEventOccurred = true;
                }
            }
            
            if (data.Keys.Count == 1 && data.ContainsKey(_qqq))
            {
                return;
            }
            
            if (!_constituentDataEncountered.ContainsKey(UtcTime.Date))
            {
                _constituentDataEncountered[UtcTime.Date] = false;
            }

            if (_constituentSymbols.Intersect(data.Keys).Any())
            {
                _constituentDataEncountered[UtcTime.Date] = true;
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_aapl, 0.5m);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_filterDateConstituentSymbolCount.Count != 2)
            {
                throw new Exception($"ETF constituent filtering function was not called 2 times (actual: {_filterDateConstituentSymbolCount.Count}");
            }
            if (!_mappingEventOccurred)
            {
                throw new Exception("No mapping/SymbolChangedEvent occurred. Expected for QQQ to be mapped from QQQQ -> QQQ");
            }

            foreach (var kvp in _filterDateConstituentSymbolCount)
            {
                if (kvp.Value < 25)
                {
                    throw new Exception($"Expected 25 or more constituents in filter function on {kvp.Key:yyyy-MM-dd HH:mm:ss.fff}, found {kvp.Value}");
                }
            }

            foreach (var kvp in _constituentDataEncountered)
            {
                if (!kvp.Value)
                {
                    throw new Exception($"Received data in OnData(...) but it did not contain any constituent data on {kvp.Key:yyyy-MM-dd HH:mm:ss.fff}");
                }
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
        public long DataPoints => 626;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-9.690%"},
            {"Drawdown", "4.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98257.31"},
            {"Net Profit", "-1.743%"},
            {"Sharpe Ratio", "-0.95"},
            {"Sortino Ratio", "-0.832"},
            {"Probabilistic Sharpe Ratio", "17.000%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.084"},
            {"Beta", "0.591"},
            {"Annual Standard Deviation", "0.078"},
            {"Annual Variance", "0.006"},
            {"Information Ratio", "-1.408"},
            {"Tracking Error", "0.065"},
            {"Treynor Ratio", "-0.125"},
            {"Total Fees", "$22.93"},
            {"Estimated Strategy Capacity", "$75000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.80%"},
            {"OrderListHash", "69695fb7639b0c1bf243eec7425a9bd2"}
        };
    }
}
