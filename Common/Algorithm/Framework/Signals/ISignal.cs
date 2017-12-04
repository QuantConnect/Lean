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
    /// Defines a signal for a single symbol.
    /// </summary>
    public interface ISignal
    {
        /// <summary>
        /// Gets the symbol this signal is for
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// Gets the type of signal, for example, price signal or volatility signal
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        SignalType Type { get; }

        /// <summary>
        /// Gets the predicted direction, down, flat or up
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        SignalDirection Direction { get; }

        /// <summary>
        /// Gets the predicted percent change in the signal type (price/volatility)
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        double? PercentChange { get; }

        /// <summary>
        /// Gets the confidence in this signal
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        double? Confidence { get; }

        /// <summary>
        /// Gets the period over which this signal is expected to come to fruition
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        TimeSpan? Period { get; }

        /// <summary>
        /// Creates a deep clone of this signal instance
        /// </summary>
        /// <returns>A new signal with identical values, but new instances</returns>
        ISignal Clone();
    }
}
