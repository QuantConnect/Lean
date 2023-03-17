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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that uses historical returns to create insights
    /// </summary>
    public class HistoricalReturnsAlphaModel : AlphaModel
    {
        private readonly int _lookback;
        private readonly Resolution _resolution;
        private readonly TimeSpan _predictionInterval;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoricalReturnsAlphaModel"/> class
        /// </summary>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="resolution">The resolution of historical data</param>
        public HistoricalReturnsAlphaModel(
            int lookback = 1,
            Resolution resolution = Resolution.Daily
            )
        {
            _lookback = lookback;
            _resolution = resolution;
            _predictionInterval = _resolution.ToTimeSpan().Multiply(_lookback);
            _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
            Name = $"{nameof(HistoricalReturnsAlphaModel)}({lookback},{resolution})";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();
            foreach (var kvp in _symbolDataBySymbol)
            {
                var symbol = kvp.Key;
                var symbolData = kvp.Value;
                if (data.Splits.ContainsKey(symbol) || data.Dividends.ContainsKey(symbol))
                {
                    symbolData.Reset();
                }

                if (symbolData.CanEmit())
                {
                    var direction = InsightDirection.Flat;
                    var magnitude = (double)symbolData.ROC.Current.Value;
                    if (magnitude > 0) direction = InsightDirection.Up;
                    if (magnitude < 0) direction = InsightDirection.Down;
                    insights.Add(Insight.Price(symbol, _predictionInterval, direction, magnitude, null));
                }
            }
            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                SymbolData symbolData;
                if (_symbolDataBySymbol.TryGetValue(removed.Symbol, out symbolData))
                {
                    _symbolDataBySymbol.Remove(removed.Symbol);
                    symbolData.Dispose();
                }
            }

            // initialize data for added securities
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                var symbol = added.Symbol;
                if (!_symbolDataBySymbol.ContainsKey(symbol))
                {
                    _symbolDataBySymbol[symbol] = new SymbolData(algorithm, symbol, _lookback, _resolution);
                    addedSymbols.Add(symbol);
                }
            }

            if (addedSymbols.Count > 0)
            {
                // warmup our indicators by pushing history through the consolidators
                algorithm.History(addedSymbols, _lookback, _resolution)
                .PushThrough(bar =>
                {
                    SymbolData symbolData;
                    if (_symbolDataBySymbol.TryGetValue(bar.Symbol, out symbolData))
                    {
                        symbolData.Update(bar);
                    }
                });
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData : IDisposable
        {
            public RateOfChange ROC;
            private QCAlgorithm _algorithm;
            private Symbol _symbol;
            private int _lookback;
            private Resolution _resolution;
            private IDataConsolidator _consolidator;
            private long _previous = 0;

            public SymbolData(QCAlgorithm algorithm, Symbol symbol, int lookback, Resolution resolution)
            {
                _algorithm = algorithm;
                _symbol = symbol;
                _lookback = lookback;
                ROC = new RateOfChange(symbol.ToString(), lookback);
                _resolution = resolution;

                SetUpConsolidator();
            }

            public bool CanEmit()
            {
                if (_previous == ROC.Samples) return false;
                _previous = ROC.Samples;
                return ROC.IsReady;
            }

            public void Update(BaseData bar)
            {
                _consolidator.Update(bar);
            }

            private void SetUpConsolidator()
            {
                _consolidator = _algorithm.ResolveConsolidator(_symbol, _resolution);
                _algorithm.RegisterIndicator(_symbol, ROC, _consolidator);
            }

            public void Reset()
            {
                ROC.Reset();
                Dispose();
                SetUpConsolidator();

                // Warm up consolidator / indicator
                var bars = _algorithm.History(_symbol, _lookback, _resolution);
                foreach (var bar in bars)
                {
                    Update(bar);
                }
            }

            public void Dispose()
            {
                _algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
            }
        }
    }
}
