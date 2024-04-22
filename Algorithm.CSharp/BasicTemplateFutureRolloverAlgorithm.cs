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
 *
*/

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm for trading continuous future
    /// </summary>
    public class BasicTemplateFutureRolloverAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Dictionary<Symbol, SymbolData> _symbolDataBySymbol = new();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 12, 10);
            SetCash(1000000);

            var futures = new List<string> {
                Futures.Indices.SP500EMini
            };

            foreach (var future in futures)
            {
                // Requesting data
                var continuousContract = AddFuture(future,
                    resolution: Resolution.Daily,
                    extendedMarketHours: true,
                    dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                    dataMappingMode: DataMappingMode.OpenInterest,
                    contractDepthOffset: 0
                );

                var symbolData = new SymbolData(this, continuousContract);
                _symbolDataBySymbol.Add(continuousContract.Symbol, symbolData);
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            foreach (var kvp in _symbolDataBySymbol)
            {
                var symbol = kvp.Key;
                var symbolData = kvp.Value;

                // Call SymbolData.Update() method to handle new data slice received
                symbolData.Update(slice);

                // Check if information in SymbolData class and new slice data are ready for trading
                if (!symbolData.IsReady || !slice.Bars.ContainsKey(symbol))
                {
                    return;
                }

                var emaCurrentValue = symbolData.EMA.Current.Value;
                if (emaCurrentValue < symbolData.Price && !symbolData.IsLong)
                {
                    MarketOrder(symbolData.Mapped, 1);
                }
                else if (emaCurrentValue > symbolData.Price && !symbolData.IsShort)
                {
                    MarketOrder(symbolData.Mapped, -1);
                }
            }
        }

        /// <summary>
        /// Abstracted class object to hold information (state, indicators, methods, etc.) from a Symbol/Security in a multi-security algorithm
        /// </summary>
        public class SymbolData
        {
            private QCAlgorithm _algorithm;
            private Future _future;
            public ExponentialMovingAverage EMA;
            public decimal Price;
            public bool IsLong;
            public bool IsShort;
            public Symbol Symbol => _future.Symbol;
            public Symbol Mapped => _future.Mapped;

            /// <summary>
            /// Check if symbolData class object are ready for trading
            /// </summary>
            public bool IsReady => Mapped != null && EMA.IsReady;

            /// <summary>
            /// Constructor to instantiate the information needed to be hold
            /// </summary>
            public SymbolData(QCAlgorithm algorithm, Future future)
            {
                _algorithm = algorithm;
                _future = future;
                EMA = algorithm.EMA(future.Symbol, 20, Resolution.Daily);

                Reset();
            }

            /// <summary>
            /// Handler of new slice of data received
            /// </summary>
            public void Update(Slice slice)
            {
                if (slice.SymbolChangedEvents.TryGetValue(Symbol, out var changedEvent))
                {
                    var oldSymbol = changedEvent.OldSymbol;
                    var newSymbol = changedEvent.NewSymbol;
                    var tag = $"Rollover - Symbol changed at {_algorithm.Time}: {oldSymbol} -> {newSymbol}";
                    var quantity = _algorithm.Portfolio[oldSymbol].Quantity;

                    // Rolling over: to liquidate any position of the old mapped contract and switch to the newly mapped contract
                    _algorithm.Liquidate(oldSymbol, tag: tag);
                    _algorithm.MarketOrder(newSymbol, quantity, tag: tag);

                    Reset();
                }

                Price = slice.Bars.ContainsKey(Symbol) ? slice.Bars[Symbol].Price : Price;
                IsLong = _algorithm.Portfolio[Mapped].IsLong;
                IsShort = _algorithm.Portfolio[Mapped].IsShort;
            }

            /// <summary>
            /// Reset RollingWindow/indicator to adapt to newly mapped contract, then warm up the RollingWindow/indicator
            /// </summary>
            private void Reset()
            {
                EMA.Reset();
                _algorithm.WarmUpIndicator(Symbol, EMA, Resolution.Daily);
            }

            /// <summary>
            /// Disposal method to remove consolidator/update method handler, and reset RollingWindow/indicator to free up memory and speed
            /// </summary>
            public void Dispose()
            {
                EMA.Reset();
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
        public long DataPoints => 1334;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 4;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0.53%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "3.011%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "0"},
            {"Start Equity", "1000000"},
            {"End Equity", "1005283.2"},
            {"Net Profit", "0.528%"},
            {"Sharpe Ratio", "1.285"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "83.704%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.015"},
            {"Beta", "-0.004"},
            {"Annual Standard Deviation", "0.011"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-4.774"},
            {"Tracking Error", "0.084"},
            {"Treynor Ratio", "-3.121"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$5900000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "0.27%"},
            {"OrderListHash", "90f952729deb9cb20be75867576e5b87"}
        };
    }  
}
