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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This indicator creates two moving averages defined on a base indicator and produces the difference
    /// between the fast and slow averages.
    /// </summary>
    public class MovingAverageConvergenceDivergence : Indicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Gets the fast average indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Fast { get; }

        /// <summary>
        /// Gets the slow average indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Slow { get; }

        /// <summary>
        /// Gets the signal of the MACD
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; }

        /// <summary>
        /// Developed by Thomas Aspray in 1986, the MACD-Histogram measures the distance between MACD and its signal line, 
        /// is an oscillator that fluctuates above and below the zero line.
        /// Bullish or bearish divergences in the MACD-Histogram can alert chartists to an imminent signal line crossover in MACD.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Histogram { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Signal.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new MACD with the specified parameters
        /// </summary>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="signalPeriod">The signal period</param>
        /// <param name="type">The type of moving averages to use</param>
        public MovingAverageConvergenceDivergence(int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Exponential)
            : this($"MACD({fastPeriod},{slowPeriod},{signalPeriod})", fastPeriod, slowPeriod, signalPeriod, type)
        {
        }

        /// <summary>
        /// Creates a new MACD with the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="signalPeriod">The signal period</param>
        /// <param name="type">The type of moving averages to use</param>
        public MovingAverageConvergenceDivergence(string name, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Exponential)
            : base(name)
        {
            if (fastPeriod >= slowPeriod)
            {
                throw new ArgumentException("MovingAverageConvergenceDivergence: fastPeriod must be less than slowPeriod", $"{nameof(fastPeriod)}, {nameof(slowPeriod)}");
            }
            
            Fast = type.AsIndicator(name + "_Fast", fastPeriod);
            Slow = type.AsIndicator(name + "_Slow", slowPeriod);
            Signal = type.AsIndicator(name + "_Signal", signalPeriod);
            Histogram = new Identity(name + "_Histogram");
            WarmUpPeriod = slowPeriod + signalPeriod - 1;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            var fastReady = Fast.Update(input);
            var slowReady = Slow.Update(input);

            var macd = Fast.Current.Value - Slow.Current.Value;

            if (fastReady && slowReady)
            {
                if (Signal.Update(input.Time, macd))
                {
                    Histogram.Update(input.Time, macd - Signal.Current.Value);
                }
            }

            return macd;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            Fast.Reset();
            Slow.Reset();
            Signal.Reset();
            Histogram.Reset();
            base.Reset();
        }
    }
}