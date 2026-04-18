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

using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Awesome Oscillator Indicator tracks the price midpoint-movement of a security. Specifically,
    /// <para>
    /// AO = MAfast[(H+L)/2] - MAslow[(H+L)/2] 
    /// </para>
    /// where MAfast and MAslow denote simple moving averages wherein fast has a shorter period.
    /// https://www.barchart.com/education/technical-indicators/awesome_oscillator
    /// </summary>
    public class AwesomeOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Gets the indicators slow period moving average.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> SlowAo { get; }

        /// <summary>
        /// Gets the indicators fast period moving average.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> FastAo { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => SlowAo.IsReady && FastAo.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new Awesome Oscillator from the specified periods.
        /// </summary>
        /// <param name="fastPeriod">The period of the fast moving average associated with the AO</param>
        /// <param name="slowPeriod">The period of the slow moving average associated with the AO</param>
        /// <param name="type">The type of moving average used when computing the fast and slow term. Defaults to simple moving average.</param>
        public AwesomeOscillator(int fastPeriod, int slowPeriod, MovingAverageType type = MovingAverageType.Simple)
            : this($"AO({fastPeriod},{slowPeriod},{type})", fastPeriod, slowPeriod, type)
        {
        }

        /// <summary>
        /// Creates a new Awesome Oscillator from the specified periods.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The period of the fast moving average associated with the AO</param>
        /// <param name="slowPeriod">The period of the slow moving average associated with the AO</param>
        /// <param name="type">The type of moving average used when computing the fast and slow term. Defaults to simple moving average.</param>
        public AwesomeOscillator(string name, int fastPeriod, int slowPeriod, MovingAverageType type = MovingAverageType.Simple)
            : base(name)
        {
            SlowAo = type.AsIndicator(slowPeriod);
            FastAo = type.AsIndicator(fastPeriod);
            WarmUpPeriod = Math.Max(slowPeriod, fastPeriod);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var presentValue = (input.High + input.Low) / 2;
            SlowAo.Update(input.EndTime, presentValue);
            FastAo.Update(input.EndTime, presentValue);

            return IsReady ? FastAo - SlowAo : 0m;
        }

        /// <summary>
        /// Resets this indicator 
        /// </summary>
        public override void Reset()
        {
            FastAo.Reset();
            SlowAo.Reset();
            base.Reset();
        }
    }
}
