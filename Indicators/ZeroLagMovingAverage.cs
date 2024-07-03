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
using MathNet.Numerics;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the zero lag moving average indicator (ZLEMA)
    /// </summary>
    public class ZeroLagMovingAverage : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// An exponential moving average is used
        /// </summary>
        private readonly int _period;
        private readonly ExponentialMovingAverage _ema;
        private readonly int _lag;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _ema.IsReady;

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _ema.Reset();
            base.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the ZeroLagMovingAverage class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the ZLEMA</param>
        public ZeroLagMovingAverage(string name, int period)
            : base(name, period)
        {
            _period = period;
            _lag = (int)Math.Round((period - 1) / 2.0);
            _ema = new ExponentialMovingAverage(name + "_EMA", period);
        }

        /// <summary>
        /// Initializes a new instance of the ZeroLagMovingAverage class with the default name and period
        /// </summary>
        /// <param name="period">The period of the ZLEMA</param>
        public ZeroLagMovingAverage(int period)
            : this($"ZLEMA({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="window">The window of data held in this indicator</param>
        /// <param name="input">The input value to this indicator on this time step</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            //RollingSum.Update(input.Time, input.Value);
            //return RollingSum.Current.Value / window.Count;
            _ema.Update(input.Time, input.Price);
            return _ema.Current.Value;
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => Period;
    }
}
