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

namespace QuantConnect.Algorithm.Framework.Signals
{
    /// <summary>
    /// Default implementation of <see cref="ISignal"/>
    /// </summary>
    public class Signal : ISignal
    {
        /// <summary>
        /// Gets the symbol this signal is for
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the type of signal, for example, price signal or volatility signal
        /// </summary>
        public SignalType Type { get; }

        /// <summary>
        /// Gets the predicted direction, down, flat or up
        /// </summary>
        public SignalDirection Direction { get; }

        /// <summary>
        /// Gets the predicted percent change in the signal type (price/volatility)
        /// </summary>
        public double? PercentChange { get; }

        /// <summary>
        /// Gets the confidence in this signal
        /// </summary>
        public double? Confidence { get; }

        /// <summary>
        /// Gets the period over which this signal is expected to come to fruition
        /// </summary>
        public TimeSpan? Period { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Signal"/> class
        /// </summary>
        /// <param name="symbol">The symbol this signal is for</param>
        /// <param name="type">The type of signal, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        public Signal(Symbol symbol, SignalType type, SignalDirection direction)
            : this(symbol, type, direction, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Signal"/> class
        /// </summary>
        /// <param name="symbol">The symbol this signal is for</param>
        /// <param name="type">The type of signal, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="percentChange">The predicted percent change</param>
        /// <param name="confidence">The confidence in this signal</param>
        /// <param name="period">The period over which the prediction will come true</param>
        public Signal(Symbol symbol, SignalType type, SignalDirection direction, double? percentChange, double? confidence, TimeSpan? period)
        {
            Symbol = symbol;
            Type = type;
            Direction = direction;
            PercentChange = percentChange;
            Confidence = confidence;
            Period = period;
        }

        /// <summary>
        /// Creates a deep clone of this signal instance
        /// </summary>
        /// <returns>A new signal with identical values, but new instances</returns>
        public ISignal Clone()
        {
            return new Signal(Symbol, Type, Direction, PercentChange, Confidence, Period);
        }
    }
}