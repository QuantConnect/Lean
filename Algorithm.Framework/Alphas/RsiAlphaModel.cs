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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Uses Wilder's RSI to create insights. Using default settings, a cross over below 30 or above 70 will
    /// trigger a new insight.
    /// </summary>
    public class RsiAlphaModel : IAlphaModel
    {
        private readonly Parameters _parameters;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();

        /// <summary>
        /// Initializes a new default instance of the <see cref="RsiAlphaModel"/> class.
        /// This uses the traditional 30/70 bounds coupled with 5% bounce protection.
        /// The traditional period of 14 days is used and the prediction interval is set to 14 days as well.
        /// </summary>
        public RsiAlphaModel()
            : this(new Parameters())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RsiAlphaModel"/> class
        /// </summary>
        /// <param name="parameters">Model parameters</param>
        public RsiAlphaModel(Parameters parameters)
        {
            _parameters = parameters;
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
            foreach (var kvp in _symbolDataBySymbol)
            {
                var symbol = kvp.Key;
                var rsi = kvp.Value.RSI;
                var previousState = kvp.Value.State;
                var state = GetState(rsi, previousState);

                if (state != previousState && rsi.IsReady)
                {
                    switch (state)
                    {
                        case State.TrippedLow:
                            insights.Add(new Insight(symbol, InsightType.Price, InsightDirection.Up, _parameters.PredictionInterval));
                            break;

                        case State.TrippedHigh:
                            insights.Add(new Insight(symbol, InsightType.Price, InsightDirection.Down, _parameters.PredictionInterval));
                            break;
                    }
                }

                kvp.Value.State = state;
            }

            return insights;
        }

        /// <summary>
        /// Cleans out old security data and initializes the RSI for any newly added securities.
        /// This functional also seeds any new indicators using a history request.
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            // clean up data for removed securities
            if (changes.RemovedSecurities.Count > 0)
            {
                var removed = changes.RemovedSecurities.ToHashSet(x => x.Symbol);
                foreach (var subscription in algorithm.SubscriptionManager.Subscriptions)
                {
                    if (removed.Contains(subscription.Symbol))
                    {
                        _symbolDataBySymbol.Remove(subscription.Symbol);
                        subscription.Consolidators.Clear();
                    }
                }
            }

            // initialize data for added securities
            if (changes.AddedSecurities.Count > 0)
            {
                var newSymbolData = new List<SymbolData>();
                foreach (var added in changes.AddedSecurities)
                {
                    if (!_symbolDataBySymbol.ContainsKey(added.Symbol))
                    {
                        var rsi = algorithm.RSI(added.Symbol, _parameters.RsiPeriod, MovingAverageType.Wilders, _parameters.Resolution);
                        var symbolData = new SymbolData(added.Symbol, rsi);
                        _symbolDataBySymbol[added.Symbol] = symbolData;
                        newSymbolData.Add(symbolData);

                        if (_parameters.Plot)
                        {
                            algorithm.PlotIndicator("RSI Alpha Model", true, rsi);
                        }
                    }
                }

                // seed new indicators using history request
                var history = algorithm.History(newSymbolData.Select(x => x.Symbol), _parameters.RsiPeriod);
                foreach (var slice in history)
                {
                    foreach (var symbol in slice.Keys)
                    {
                        var value = slice[symbol];
                        var list = value as IList;
                        var data = (BaseData) (list != null ? list[list.Count - 1] : value);

                        SymbolData symbolData;
                        if (_symbolDataBySymbol.TryGetValue(symbol, out symbolData))
                        {
                            symbolData.RSI.Update(data.EndTime, data.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines the new state. This is basically cross-over detection logic that
        /// includes considerations for bouncing using the configured bounce tolerance.
        /// </summary>
        private State GetState(RelativeStrengthIndex rsi, State previous)
        {
            if (rsi > _parameters.UpperRsiBound)
            {
                return State.TrippedHigh;
            }

            if (rsi < _parameters.LowerRsiBound)
            {
                return State.TrippedLow;
            }

            if (previous == State.TrippedLow)
            {
                if (rsi > _parameters.LowerRsiBound + _parameters.BounceTolerance)
                {
                    return State.Middle;
                }
            }

            if (previous == State.TrippedHigh)
            {
                if (rsi < _parameters.UpperRsiBound - _parameters.BounceTolerance)
                {
                    return State.Middle;
                }
            }

            return previous;
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        private class SymbolData
        {
            public Symbol Symbol { get; }
            public State State { get; set; }
            public RelativeStrengthIndex RSI { get; }

            public SymbolData(Symbol symbol, RelativeStrengthIndex rsi)
            {
                Symbol = symbol;
                RSI = rsi;
                State = State.Middle;
            }
        }

        /// <summary>
        /// Defines the state. This is used to prevent signal spamming and aid in bounce detection.
        /// </summary>
        private enum State
        {
            TrippedLow,
            Middle,
            TrippedHigh
        }

        public class Parameters
        {
            /// <summary>
            /// RSI indicator resolution
            /// </summary>
            public Resolution Resolution { get; set; } = Resolution.Daily;

            /// <summary>
            /// Generated insight prediction interval
            /// </summary>
            public TimeSpan PredictionInterval { get; set; } = TimeSpan.FromDays(14);

            /// <summary>
            /// RSI period
            /// </summary>
            public int RsiPeriod { get; set; } = 14;

            /// <summary>
            /// RSI lower bound. Values below this will trigger an UP prediction.
            /// </summary>
            public decimal LowerRsiBound { get; set; } = 30;

            /// <summary>
            /// RSI upper bound. Values above this will trigger a DOWN prediction.
            /// </summary>
            public decimal UpperRsiBound { get; set; } = 70;

            /// <summary>
            /// Plots the indicator values
            /// </summary>
            public bool Plot { get; set; } = false;

            /// <summary>
            /// Before allowing another signal to be generated, we must cross-over this
            /// tolernce towards 50. For example, if we just crossed below the lower bound
            /// (nominally 30), we won't interpret another crossing until it moves above
            /// 35 (lower bound + tolerance). Likewise for the upper bound, just that we
            /// subtract, nominally 70 - 5 = 65.
            /// </summary>
            public decimal BounceTolerance { get; set; } = 5;

            /// <summary>
            /// Initializes a new default instance of the <see cref="Parameters"/> class
            /// </summary>
            public Parameters()
            {
            }

            /// <summary>
            /// Intializes a new instance of the <see cref="Parameters"/> class
            /// </summary>
            /// <param name="resolution">The RSI indicator resolution</param>
            /// <param name="rsiPeriod">The RSI indicator period</param>
            /// <param name="lowerRsiBound">The RSI lower bound, used to signal UP insights</param>
            /// <param name="upperRsiBound">The RSI upper bound, used to signal DOWN insights</param>
            /// <param name="predictionInterval">The period applied to each generated insight</param>
            public Parameters(Resolution resolution, int rsiPeriod, decimal lowerRsiBound, decimal upperRsiBound, TimeSpan predictionInterval)
            {
                Resolution = resolution;
                RsiPeriod = rsiPeriod;
                LowerRsiBound = lowerRsiBound;
                UpperRsiBound = upperRsiBound;
                PredictionInterval = predictionInterval;
            }
        }
    }
}