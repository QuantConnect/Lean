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
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Asserts that the fundamental data delivered by a chained universe (a universe not based on fundamental data, filtered by a
    /// fundamental selection function) is the data of the previous trading day, matching the last close received through OnData
    /// and the rest of the fundamental APIs. It must not carry the selection date itself, which would be look-ahead bias.
    /// Reproduces https://github.com/QuantConnect/Lean/issues/9793
    /// </summary>
    public class ChainedFundamentalUniverseSelectionTimeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private readonly Dictionary<Symbol, TradeBar> _lastBars = new();
        private int _selectionCount;
        private int _priceAssertionCount;

        public override void Initialize()
        {
            SetStartDate(2014, 3, 25);
            // includes 2 mondays, whose previous trading day is not the previous calendar day
            SetEndDate(2014, 4, 7);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            // so we can compare the raw fundamental price with the daily bars close
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            var spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var symbols = new[] { QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA), spy };

            // a scheduled universe, not based on fundamental data, chained with a fundamental selection function
            var universe = new ScheduledUniverse(DateRules.EveryDay(spy), TimeRules.Midnight, _ => symbols, UniverseSettings);

            AddUniverse(universe, FundamentalFilter);
        }

        private IEnumerable<Symbol> FundamentalFilter(IEnumerable<Fundamental> fundamentals)
        {
            _selectionCount++;

            var selected = new List<Symbol>();
            foreach (var fundamental in fundamentals)
            {
                var symbol = fundamental.Symbol;
                // The fundamental data for a date is only available the next day, so the data we get is the previous day's,
                // available at the current selection time
                if (fundamental.Time != Time.AddDays(-1) || fundamental.EndTime != Time)
                {
                    throw new RegressionTestException($"Unexpected {symbol} fundamental time in chained universe selection at {Time}: " +
                        $"Time {fundamental.Time}, EndTime {fundamental.EndTime}");
                }

                // it should match the fundamental data we can get directly through the algorithm API at this point in time
                var fundamentalThroughAlgo = Fundamentals(symbol);
                if (fundamentalThroughAlgo.Time != fundamental.Time
                    || fundamentalThroughAlgo.EndTime != fundamental.EndTime
                    || fundamentalThroughAlgo.Price != fundamental.Price)
                {
                    throw new RegressionTestException($"Chained universe selection {symbol} fundamental data {fundamental.Time} {fundamental.Price} " +
                        $"does not match the algorithm API fundamental data {fundamentalThroughAlgo.Time} {fundamentalThroughAlgo.Price}");
                }

                // the fundamental price and volume should match the last daily bar we received, the previous trading day's.
                // On mondays the fundamental data is dated sunday, the data provider forward fills friday's data
                if (_lastBars.TryGetValue(symbol, out var lastBar))
                {
                    if (lastBar.Close != fundamental.Price
                        || lastBar.Close != fundamental.Value
                        || lastBar.Volume != fundamental.Volume)
                    {
                        throw new RegressionTestException($"Chained universe selection {symbol} fundamental data at {Time}: {fundamental.Time} " +
                            $"price {fundamental.Price} volume {fundamental.Volume} does not match the last bar {lastBar.Time} " +
                            $"close {lastBar.Close} volume {lastBar.Volume}");
                    }
                    _priceAssertionCount++;
                }
                else if (fundamental.Price == 0)
                {
                    throw new RegressionTestException($"Unexpected {symbol} fundamental price at {Time}");
                }

                selected.Add(symbol);
            }

            if (selected.Count != 2)
            {
                throw new RegressionTestException($"Unexpected fundamental data count {selected.Count} in chained universe selection at {Time}");
            }

            return selected;
        }

        public override void OnData(Slice slice)
        {
            foreach (var bar in slice.Bars.Values)
            {
                _lastBars[bar.Symbol] = bar;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            // one selection per trading day
            if (_selectionCount != 10)
            {
                throw new RegressionTestException($"Unexpected chained universe selection count {_selectionCount}");
            }
            // the first selection happens before any bar is received
            if (_priceAssertionCount != 18)
            {
                throw new RegressionTestException($"Unexpected fundamental price assertion count {_priceAssertionCount}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 117;

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
            {"Information Ratio", "1.868"},
            {"Tracking Error", "0.098"},
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
