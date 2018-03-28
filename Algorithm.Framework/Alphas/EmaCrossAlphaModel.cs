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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that uses an EMA cross to create insights
    /// </summary>
    public class EmaCrossAlphaModel : IAlphaModel
    {
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;
        private readonly TimeSpan _predictionInterval;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmaCrossAlphaModel"/> class
        /// </summary>
        /// <param name="fastPeriod">The fast EMA period</param>
        /// <param name="slowPeriod">The slow EMA period</param>
        /// <param name="predictionInterval">The interval over which we're predicting</param>
        public EmaCrossAlphaModel(int fastPeriod, int slowPeriod, TimeSpan predictionInterval)
        {
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;
            _predictionInterval = predictionInterval;
            _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            var insights = new List<Insight>();
            foreach (var symbolData in _symbolDataBySymbol.Values)
            {
                if (symbolData.Fast.IsReady && symbolData.Slow.IsReady)
                {
                    if (symbolData.FastIsOverSlow)
                    {
                        if (symbolData.Slow > symbolData.Fast)
                        {
                            insights.Add(new Insight(symbolData.Symbol, InsightType.Price, InsightDirection.Down, _predictionInterval));
                        }
                    }
                    else if (symbolData.SlowIsOverFast)
                    {
                        if (symbolData.Fast > symbolData.Slow)
                        {
                            insights.Add(new Insight(symbolData.Symbol, InsightType.Price, InsightDirection.Up, _predictionInterval));
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
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                SymbolData symbolData;
                if (!_symbolDataBySymbol.TryGetValue(added.Symbol, out symbolData))
                {
                    // create fast/slow EMAs
                    var fast = algorithm.EMA(added.Symbol, _fastPeriod);
                    var slow = algorithm.EMA(added.Symbol, _slowPeriod);
                    _symbolDataBySymbol[added.Symbol] = new SymbolData
                    {
                        Security = added,
                        Fast = fast,
                        Slow = slow
                    };
                }
                else
                {
                    // a security that was already initialized was re-added, reset the indicators
                    symbolData.Fast.Reset();
                    symbolData.Slow.Reset();
                }
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData
        {
            public Security Security { get; set; }
            public Symbol Symbol => Security.Symbol;
            public ExponentialMovingAverage Fast { get; set; }
            public ExponentialMovingAverage Slow { get; set; }

            /// <summary>
            /// True if the fast is above the slow, otherwise false.
            /// This is used to prevent emitting the same signal repeatedly
            /// </summary>
            public bool FastIsOverSlow { get; set; }
            public bool SlowIsOverFast => !FastIsOverSlow;
        }
    }
}