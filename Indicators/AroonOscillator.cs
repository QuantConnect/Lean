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
    /// The Aroon Oscillator is the difference between AroonUp and AroonDown. The value of this
    /// indicator fluctuates between -100 and +100. An upward trend bias is present when the oscillator
    /// is positive, and a negative trend bias is present when the oscillator is negative. AroonUp/Down
    /// values over 75 identify strong trends in their respective direction.
    /// </summary>
    public class AroonOscillator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Gets the AroonUp indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> AroonUp { get; }

        /// <summary>
        /// Gets the AroonDown indicator
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> AroonDown { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => AroonUp.IsReady && AroonDown.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new AroonOscillator from the specified up/down periods.
        /// </summary>
        /// <param name="upPeriod">The lookback period to determine the highest high for the AroonDown</param>
        /// <param name="downPeriod">The lookback period to determine the lowest low for the AroonUp</param>
        public AroonOscillator(int upPeriod, int downPeriod)
            : this($"AROON({upPeriod},{downPeriod})", upPeriod, downPeriod)
        {
        }

        /// <summary>
        /// Creates a new AroonOscillator from the specified up/down periods.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="upPeriod">The lookback period to determine the highest high for the AroonDown</param>
        /// <param name="downPeriod">The lookback period to determine the lowest low for the AroonUp</param>
        public AroonOscillator(string name, int upPeriod, int downPeriod)
            : base(name)
        {
            var max = new Maximum(name + "_Max", upPeriod + 1);
            AroonUp = new FunctionalIndicator<IndicatorDataPoint>(name + "_AroonUp",
                input => ComputeAroonUp(upPeriod, max, input),
                aroonUp => max.IsReady,
                () => max.Reset()
                );

            var min = new Minimum(name + "_Min", downPeriod + 1);
            AroonDown = new FunctionalIndicator<IndicatorDataPoint>(name + "_AroonDown",
                input => ComputeAroonDown(downPeriod, min, input),
                aroonDown => min.IsReady,
                () => min.Reset()
                );

            WarmUpPeriod = 1 + Math.Max(upPeriod, downPeriod);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            AroonUp.Update(input.Time, input.High);
            AroonDown.Update(input.Time, input.Low);

            return AroonUp.Current.Value - AroonDown.Current.Value;
        }

        /// <summary>
        /// AroonUp = 100 * (period - {periods since max})/period
        /// </summary>
        /// <param name="upPeriod">The AroonUp period</param>
        /// <param name="max">A Maximum indicator used to compute periods since max</param>
        /// <param name="input">The next input data</param>
        /// <returns>The AroonUp value</returns>
        private static decimal ComputeAroonUp(int upPeriod, Maximum max, IndicatorDataPoint input)
        {
            max.Update(input);
            return 100m * (upPeriod - max.PeriodsSinceMaximum) / upPeriod;
        }

        /// <summary>
        /// AroonDown = 100 * (period - {periods since min})/period
        /// </summary>
        /// <param name="downPeriod">The AroonDown period</param>
        /// <param name="min">A Minimum indicator used to compute periods since min</param>
        /// <param name="input">The next input data</param>
        /// <returns>The AroonDown value</returns>
        private static decimal ComputeAroonDown(int downPeriod, Minimum min, IndicatorDataPoint input)
        {
            min.Update(input);
            return 100m * (downPeriod - min.PeriodsSinceMinimum) / downPeriod;
        }

        /// <summary>
        /// Resets this indicator and both sub-indicators (AroonUp and AroonDown)
        /// </summary>
        public override void Reset()
        {
            AroonUp.Reset();
            AroonDown.Reset();
            base.Reset();
        }
    }
}