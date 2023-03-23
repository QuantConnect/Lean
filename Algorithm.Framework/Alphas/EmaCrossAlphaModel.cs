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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using System;
using static QuantConnect.Messages;
using System.Security.Cryptography;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that uses an EMA cross to create insights
    /// </summary>
    public class EmaCrossAlphaModel : AlphaModel
    {
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;
        private readonly Resolution _resolution;
        private readonly int _predictionInterval;

        /// <summary>
        /// This is made protected for testing purposes
        /// </summary>
        protected readonly Dictionary<Symbol, SymbolData> SymbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmaCrossAlphaModel"/> class
        /// </summary>
        /// <param name="fastPeriod">The fast EMA period</param>
        /// <param name="slowPeriod">The slow EMA period</param>
        /// <param name="resolution">The resolution of data sent into the EMA indicators</param>
        public EmaCrossAlphaModel(
            int fastPeriod = 12,
            int slowPeriod = 26,
            Resolution resolution = Resolution.Daily
            )
        {
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;
            _resolution = resolution;
            _predictionInterval = fastPeriod;
            SymbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
            Name = $"{nameof(EmaCrossAlphaModel)}({fastPeriod},{slowPeriod},{resolution})";
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
            foreach (var kvp in SymbolDataBySymbol)
            {
                var symbol = kvp.Key;
                var symbolData = kvp.Value;
                if (symbolData.Fast.IsReady && symbolData.Slow.IsReady)
                {
                    var insightPeriod = _resolution.ToTimeSpan().Multiply(_predictionInterval);
                    if (symbolData.FastIsOverSlow)
                    {
                        if (symbolData.Slow > symbolData.Fast)
                        {
                            insights.Add(Insight.Price(symbol, insightPeriod, InsightDirection.Down));
                        }
                    }
                    else if (symbolData.SlowIsOverFast)
                    {
                        if (symbolData.Fast > symbolData.Slow)
                        {
                            insights.Add(Insight.Price(symbol, insightPeriod, InsightDirection.Up));
                        }
                    }
                }

                symbolData.FastIsOverSlow = symbolData.Fast > symbolData.Slow;
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
            var addedSymbols = new List<Symbol>();
            foreach (var security in changes.AddedSecurities)
            {
                var symbol = security.Symbol;
                SymbolData symbolData;
                if (!SymbolDataBySymbol.TryGetValue(symbol, out symbolData))
                {
                    SymbolDataBySymbol[symbol] = new SymbolData(symbol, _fastPeriod, _slowPeriod, algorithm, _resolution);
                    addedSymbols.Add(symbol);
                }
            }

            if (addedSymbols.Count > 0)
            {
                // warmup our indicators by pushing history through the consolidators
                algorithm.History(addedSymbols, _slowPeriod, _resolution)
                    .PushThrough(data =>
                    {
                        SymbolData symbolData;
                        if (SymbolDataBySymbol.TryGetValue(data.Symbol, out symbolData))
                        {
                            symbolData.Update(data);
                        }
                    });
            }

            foreach (var security in changes.RemovedSecurities)
            {
                SymbolData symbolData;
                if (SymbolDataBySymbol.TryGetValue(security.Symbol, out symbolData))
                {
                    // clean up our consolidators
                    symbolData.Dispose();
                    SymbolDataBySymbol.Remove(security.Symbol);
                }
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        public class SymbolData : IDisposable
        {
            private readonly QCAlgorithm _algorithm;
            private readonly IDataConsolidator _consolidator;
            private readonly ExponentialMovingAverage _fast;
            private readonly ExponentialMovingAverage _slow;
            private readonly Symbol _symbol;

            public ExponentialMovingAverage Fast => _fast;
            public ExponentialMovingAverage Slow => _slow;

            /// <summary>
            /// True if the fast is above the slow, otherwise false.
            /// This is used to prevent emitting the same signal repeatedly
            /// </summary>
            public bool FastIsOverSlow { get; set; }
            public bool SlowIsOverFast => !FastIsOverSlow;

            public SymbolData(
                Symbol symbol,
                int fastPeriod,
                int slowPeriod,
                QCAlgorithm algorithm,
                Resolution resolution)
            {
                _symbol = symbol;
                _algorithm = algorithm;

                // create fast/slow EMAs
                _fast = new ExponentialMovingAverage(symbol, fastPeriod, ExponentialMovingAverage.SmoothingFactorDefault(fastPeriod));
                _slow = new ExponentialMovingAverage(symbol, slowPeriod, ExponentialMovingAverage.SmoothingFactorDefault(slowPeriod));

                // Create a consolidator to update the EMAs over time
                _consolidator = algorithm.ResolveConsolidator(symbol, resolution);
                _consolidator.DataConsolidated += ConsolidationHandler;
                algorithm.SubscriptionManager.AddConsolidator(symbol, _consolidator);
            }

            /// <summary>
            /// Event handler for when the consolidator produces a new consolidated bar
            /// </summary>
            /// <param name="sender">The consolidator object that produced the bar</param>
            /// <param name="bar">The consolidated bar</param>
            public void ConsolidationHandler(object sender, IBaseData bar)
            {
                _fast.Update(bar.EndTime, bar.Value);
                _slow.Update(bar.EndTime, bar.Value);
            }

            /// <summary>
            /// A method to warm up indicators by feeding historical data into the consolidator
            /// </summary>
            /// <param name="bar">A historical bar of data</param>
            public void Update(BaseData bar)
            {
                _consolidator.Update(bar);
            }

            /// <summary>
            /// Removes consolidators
            /// </summary>
            public void Dispose()
            {
                _algorithm.SubscriptionManager.RemoveConsolidator(_symbol, _consolidator);
            }
        }
    }
}
