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
    public class MovingAverageConvergenceDivergence : Indicator
    {
        /// <summary>
        /// Gets the fast average indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Fast { get; private set; }

        /// <summary>
        /// Gets the slow average indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Slow { get; private set; }

        /// <summary>
        /// Gets the signal of the MACD
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Signal { get; private set; }

        /// <summary>
        /// Developed by Thomas Aspray in 1986, the MACD-Histogram measures the distance between MACD and its signal line, 
        /// is an oscillator that fluctuates above and below the zero line.
        /// Bullish or bearish divergences in the MACD-Histogram can alert chartists to an imminent signal line crossover in MACD.
        /// </summary>
        public decimal Histogram { get; private set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Signal.IsReady; }
        }

        /// <summary>
        /// Creates a new MACD with the specified parameters
        /// </summary>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="signalPeriod">The signal period</param>
        /// <param name="type">The type of moving averages to use</param>
        public MovingAverageConvergenceDivergence(int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Simple)
            : this(string.Format("MACD({0},{1})", fastPeriod, slowPeriod), fastPeriod, slowPeriod, signalPeriod, type)
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
        public MovingAverageConvergenceDivergence(string name, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Simple)
            : base(name)
        {
            Fast = type.AsIndicator(name + "_Fast", fastPeriod);
            Slow = type.AsIndicator(name + "_Slow", slowPeriod);
            Signal = type.AsIndicator(name + "_Signal", signalPeriod);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            Fast.Update(input);
            Slow.Update(input);

            var macd = Fast - Slow;
            if (Fast.IsReady && Slow.IsReady)
            {
                Signal.Update(input.Time, macd);
            }

            Histogram = macd - Signal;
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
            Histogram = Decimal.Zero;
            base.Reset();
        }
    }
}