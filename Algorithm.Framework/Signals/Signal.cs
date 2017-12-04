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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Algorithm.Framework.Signals
{
    /// <summary>
    /// Default implementation of <see cref="ISignal"/>
    /// </summary>
    public class Signal : ISignal, IEquatable<Signal>
    {
        /// <summary>
        /// Gets the symbol this signal is for
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the type of signal, for example, price signal or volatility signal
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SignalType Type { get; }

        /// <summary>
        /// Gets the predicted direction, down, flat or up
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SignalDirection Direction { get; }

        /// <summary>
        /// Gets the predicted percent change in the signal type (price/volatility)
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? PercentChange { get; }

        /// <summary>
        /// Gets the confidence in this signal
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? Confidence { get; }

        /// <summary>
        /// Gets the period over which this signal is expected to come to fruition
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
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

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Signal other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Symbol, other.Symbol) &&
                Direction == other.Direction &&
                Type == other.Type &&
                Confidence.Equals(other.Confidence) &&
                PercentChange.Equals(other.PercentChange) &&
                Period.Equals(other.Period);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Signal)obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Symbol != null ? Symbol.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (int)Direction;
                hashCode = (hashCode * 397) ^ PercentChange.GetHashCode();
                hashCode = (hashCode * 397) ^ Confidence.GetHashCode();
                hashCode = (hashCode * 397) ^ Period.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines if the two signals are equal
        /// </summary>
        public static bool operator ==(ISignal left, Signal right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines if the two signals are equal
        /// </summary>
        public static bool operator ==(Signal left, Signal right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines if the two signals are equal
        /// </summary>
        public static bool operator ==(Signal left, ISignal right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines if the two signals are not equal
        /// </summary>
        public static bool operator !=(ISignal left, Signal right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Determines if the two signals are not equal
        /// </summary>
        public static bool operator !=(Signal left, Signal right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Determines if the two signals are not equal
        /// </summary>
        public static bool operator !=(Signal left, ISignal right)
        {
            return !Equals(left, right);
        }
    }
}