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
using QuantConnect.Indicators;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Uses Wilder's RSI to create insights. Using default settings, a cross over below 30 or above 70 will
    /// trigger a new insight.
    /// </summary>
    public class RsiAlphaModel : AlphaModel
    {
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();

        private readonly int _period;
        private readonly Resolution _resolution;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsiAlphaModel"/> class
        /// </summary>
        /// <param name="period">The RSI indicator period</param>
        /// <param name="resolution">The resolution of data sent into the RSI indicator</param>
        public RsiAlphaModel(
            int period = 14,
            Resolution resolution = Resolution.Daily
            )
        {
            _period = period;
            _resolution = resolution;
            Name = $"{nameof(RsiAlphaModel)}({_period},{_resolution})";
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
                var rsi = kvp.Value.RSI;
                var previousState = kvp.Value.State;
                var state = GetState(rsi, previousState);

                if (state != previousState && rsi.IsReady)
                {
                    var insightPeriod = _resolution.ToTimeSpan().Multiply(_period);

                    switch (state)
                    {
                        case State.TrippedLow:
                            insights.Add(Insight.Price(symbol, insightPeriod, InsightDirection.Up));
                            break;

                        case State.TrippedHigh:
                            insights.Add(Insight.Price(symbol, insightPeriod, InsightDirection.Down));
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
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
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
            var addedSymbols = new List<Symbol>();
            foreach (var added in changes.AddedSecurities)
            {
                if (!_symbolDataBySymbol.ContainsKey(added.Symbol))
                {
                    var rsi = algorithm.RSI(added.Symbol, _period, MovingAverageType.Wilders, _resolution);
                    var symbolData = new SymbolData(added.Symbol, rsi);
                    _symbolDataBySymbol[added.Symbol] = symbolData;
                    addedSymbols.Add(symbolData.Symbol);
                }
            }

            if (addedSymbols.Count > 0)
            {
                // warmup our indicators by pushing history through the consolidators
                algorithm.History(addedSymbols, _period, _resolution)
                    .PushThrough(data =>
                    {
                        SymbolData symbolData;
                        if (_symbolDataBySymbol.TryGetValue(data.Symbol, out symbolData))
                        {
                            symbolData.RSI.Update(data.EndTime, data.Value);
                        }
                    });
            }
        }

        /// <summary>
        /// Determines the new state. This is basically cross-over detection logic that
        /// includes considerations for bouncing using the configured bounce tolerance.
        /// </summary>
        private State GetState(RelativeStrengthIndex rsi, State previous)
        {
            if (rsi > 70m)
            {
                return State.TrippedHigh;
            }

            if (rsi < 30m)
            {
                return State.TrippedLow;
            }

            if (previous == State.TrippedLow)
            {
                if (rsi > 35m)
                {
                    return State.Middle;
                }
            }

            if (previous == State.TrippedHigh)
            {
                if (rsi < 65m)
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
    }
}