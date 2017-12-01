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
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Signals
{
    /// <summary>
    /// Provides an implementation of <see cref="ISignalModel"/> that always returns the same signal for each security
    /// </summary>
    public class ConstantSignalModel : ISignalModel
    {
        private readonly SignalType _type;
        private readonly SignalDirection _direction;
        private readonly double? _percentChange;
        private readonly double? _confidence;
        private readonly TimeSpan? _period;
        private readonly HashSet<Security> _securities = new HashSet<Security>();
        private readonly HashSet<Symbol> _signalsSent = new HashSet<Symbol>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantSignalModel"/> class
        /// </summary>
        /// <param name="type">The type of signal</param>
        /// <param name="direction">The direction of the signal</param>
        public ConstantSignalModel(SignalType type, SignalDirection direction)
            : this(type, direction, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantSignalModel"/> class
        /// </summary>
        /// <param name="type">The type of signal</param>
        /// <param name="direction">The direction of the signal</param>
        /// <param name="percentChange">The predicted percent change</param>
        /// <param name="confidence">The confidence in the signal</param>
        /// <param name="period">The period over which the signal with come to fruition</param>
        public ConstantSignalModel(SignalType type, SignalDirection direction, double? percentChange, double? confidence, TimeSpan? period)
        {
            _type = type;
            _direction = direction;
            _percentChange = percentChange;
            _confidence = confidence;
            _period = period;
        }

        /// <summary>
        /// Creates a constant signal for each security as specified via the constructor
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new signals generated</returns>
        public IEnumerable<Signal> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            // we're only trying to send the up signal once per security
            // we'll send the signal again if they're removed and then re-added
            return _securities.Where(security => _signalsSent.Add(security.Symbol))
                .Select(security => new Signal(
                    security.Symbol,
                    _type,
                    _direction,
                    _percentChange,
                    _confidence,
                    _period
                ));
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            NotifiedSecurityChanges.UpdateCollection(_securities, changes);

            // this will allow the signal to be re-sent when the security re-joins the universe
            foreach (var removed in changes.RemovedSecurities)
            {
                _signalsSent.Remove(removed.Symbol);
            }
        }
    }
}
