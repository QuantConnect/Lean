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
        private readonly  InsightCollection _insightCollection;

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
            _insightCollection = new InsightCollection();
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
            foreach (var (symbol, symbolData) in _symbolDataBySymbol)
            {
                symbolData.HandleCorporateActions(data);

                if (symbolData.CanEmit())
                {
                    var direction = InsightDirection.Flat;
                    var magnitude = (double)symbolData.ROC.Current.Value;
                    if (magnitude > 0) direction = InsightDirection.Up;
                    if (magnitude < 0) direction = InsightDirection.Down;
                    
                    if (direction == InsightDirection.Flat)
                    {
                        CancelInsights(algorithm, symbol);
                        continue;
                    }

                    insights.Add(Insight.Price(symbol, _predictionInterval, direction, magnitude));
                }
            }
            _insightCollection.AddRange(insights);
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
                if (_symbolDataBySymbol.TryGetValue(removed.Symbol, out var data))
                {
                    _symbolDataBySymbol.Remove(removed.Symbol);
                    data.Dispose();   
                }

                CancelInsights(algorithm, removed.Symbol);
            }

            // Indicators must be updated with scaled raw data to avoid price jumps
            var dataNormalizationMode = algorithm.UniverseSettings.DataNormalizationMode;
            if (dataNormalizationMode == DataNormalizationMode.Raw)
            {
                dataNormalizationMode = DataNormalizationMode.ScaledRaw;
            }

            // initialize data for added securities
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataBySymbol.ContainsKey(added.Symbol))
                {
                    var symbolData = new SymbolData(algorithm, added.Symbol, _lookback, _resolution, dataNormalizationMode);
                    _symbolDataBySymbol[added.Symbol] = symbolData;
                    addedSymbols.Add(added.Symbol);
                }
            }

            if (addedSymbols.Count > 0)
            {
                // warmup our indicators by pushing history through the consolidators
                algorithm.History(addedSymbols, _lookback, _resolution, dataNormalizationMode: dataNormalizationMode)
                    .PushThrough(bar =>
                    {
                        if (_symbolDataBySymbol.TryGetValue(bar.Symbol, out var symbolData))
                        {
                            symbolData.ROC.Update(bar.EndTime, bar.Value);
                        }
                    });
            }
        }

        private void CancelInsights(QCAlgorithm algorithm, Symbol symbol)
        {
            if (_insightCollection.TryGetValue(symbol, out var insights))
            {
                algorithm.Insights.Cancel(insights);
                _insightCollection.Clear(new[] { symbol });
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData : IDisposable
        {
            private long _previous;
            private QCAlgorithm _algorithm;
            private Symbol _symbol;
            private IDataConsolidator _consolidator;
            public RateOfChange ROC;
            public readonly Action<Slice> HandleCorporateActions;

            public SymbolData(QCAlgorithm algorithm, Symbol symbol, int lookback, Resolution resolution, DataNormalizationMode dataNormalizationMode)
            {
                _algorithm = algorithm;
                _symbol = symbol;
                _consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                ROC = new RateOfChange($"{symbol}.ROC({lookback})", lookback);
                algorithm.RegisterIndicator(symbol, ROC, _consolidator);

                HandleCorporateActions = slice =>
                {
                    if (slice.Splits.ContainsKey(symbol) || slice.Dividends.ContainsKey(symbol))
                    {
                        // We need to keep the relative difference between samples and period
                        var delta = ROC.Samples - _previous;

                        ROC.Reset();

                        // warmup our indicators by pushing history through the consolidators
                        algorithm.History(new[] { symbol }, ROC.WarmUpPeriod, resolution, 
                                dataNormalizationMode: dataNormalizationMode)
                            .PushThrough(bar =>
                                {
                                    ROC.Update(bar.EndTime, bar.Value);
                                }
                            );

                        _previous = ROC.Samples + delta;
                    }
                };
            }

            public bool CanEmit()
            {
                if (_previous == ROC.Samples) return false;
                _previous = ROC.Samples;
                return ROC.IsReady;
            }

            public void Dispose()
            {
                ROC.Reset();
                _algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
            }
        }
    }
}
