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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Signals
{
    /// <summary>
    /// Defines a custom signal model that uses MACD crossovers. The signal line is
    /// used to generate buy/sell signals if it's stronger than the bounce threshold.
    /// If the signal is within the bounce threshold then a flat price signal is returned.
    /// </summary>
    public class MACDSignalModel : ISignalModel
    {
        private readonly TimeSpan _signalPeriod;
        private readonly decimal _bounceThresholdPercent;
        private readonly Dictionary<Symbol, SymbolData> _symbolData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MACDSignalModel"/> class
        /// </summary>
        /// <param name="signalPeriod">The period of the MACD's input</param>
        /// <param name="bounceThresholdPercent">The percent change required in the signal to warrant an up/down signal</param>
        public MACDSignalModel(TimeSpan signalPeriod, decimal bounceThresholdPercent)
        {
            _signalPeriod = signalPeriod;
            _bounceThresholdPercent = Math.Abs(bounceThresholdPercent);
            _symbolData = new Dictionary<Symbol, SymbolData>();
        }

        /// <summary>
        /// Determines a signal for each security based on it's current MACD signal
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new signals generated</returns>
        public IEnumerable<Signal> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            foreach (var sd in _symbolData.Values)
            {
                if (sd.Security.Price == 0)
                {
                    continue;
                }

                var direction = SignalDirection.Flat;
                var normalizedSignal = sd.MACD.Signal / sd.Security.Price;
                if (normalizedSignal > _bounceThresholdPercent)
                {
                    direction = SignalDirection.Up;
                }
                else if (normalizedSignal < -_bounceThresholdPercent)
                {
                    direction = SignalDirection.Down;
                }

                var signal = new Signal(sd.Security.Symbol, SignalType.Price, direction);
                if (signal.Equals(sd.PreviousSignal))
                {
                    continue;
                }

                sd.PreviousSignal = signal.Clone();
                yield return signal;
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed.
        /// This initializes the MACD for each added security and cleans up the indicator for each removed security.
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                _symbolData.Add(added.Symbol, new SymbolData(algorithm, added, _signalPeriod));
            }
            foreach (var removed in changes.RemovedSecurities)
            {
                SymbolData data;
                if (_symbolData.TryGetValue(removed.Symbol, out data))
                {
                    data.CleanUp(algorithm);
                    _symbolData.Remove(removed.Symbol);
                }
            }
        }

        class SymbolData
        {
            public ISignal PreviousSignal;

            public readonly Security Security;
            public readonly IDataConsolidator Consolidator;
            public readonly MovingAverageConvergenceDivergence MACD;

            public SymbolData(QCAlgorithmFramework algorithm, Security security, TimeSpan period)
            {
                Security = security;
                Consolidator = algorithm.ResolveConsolidator(security.Symbol, period);
                algorithm.SubscriptionManager.AddConsolidator(security.Symbol, Consolidator);

                MACD = new MovingAverageConvergenceDivergence(12, 26, 9, MovingAverageType.Exponential);

                Consolidator.DataConsolidated += OnDataConsolidated;
            }

            /// <summary>
            /// Cleans up the indicator and consolidator
            /// </summary>
            /// <param name="algorithm">The algorithm instance</param>
            public void CleanUp(QCAlgorithmFramework algorithm)
            {
                Consolidator.DataConsolidated -= OnDataConsolidated;
                algorithm.SubscriptionManager.RemoveConsolidator(Security.Symbol, Consolidator);
            }

            private void OnDataConsolidated(object sender, IBaseData consolidated)
            {
                MACD.Update(consolidated.EndTime, consolidated.Value);
            }
        }
    }
}
