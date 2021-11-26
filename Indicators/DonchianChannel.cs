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
    /// This indicator computes the upper and lower band of the Donchian Channel.
    /// The upper band is computed by finding the highest high over the given period.
    /// The lower band is computed by finding the lowest low over the given period.
    /// The primary output value of the indicator is the mean of the upper and lower band for 
    /// the given timeframe.
    /// </summary>
    public class DonchianChannel : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private IBaseDataBar _previousInput;

        /// <summary>
        /// Gets the upper band of the Donchian Channel.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UpperBand { get; }

        /// <summary>
        /// Gets the lower band of the Donchian Channel.
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> LowerBand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DonchianChannel"/> class.
        /// </summary>
        /// <param name="period">The period for both the upper and lower channels.</param>
        public DonchianChannel(int period)
            : this(period, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DonchianChannel"/> class.
        /// </summary>
        /// <param name="upperPeriod">The period for the upper channel.</param>
        /// <param name="lowerPeriod">The period for the lower channel</param>
        public DonchianChannel(int upperPeriod, int lowerPeriod)
            : this($"DCH({lowerPeriod},{lowerPeriod})", upperPeriod, lowerPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DonchianChannel"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="period">The period for both the upper and lower channels.</param>
        public DonchianChannel(string name, int period)
            : this(name, period, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DonchianChannel"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="upperPeriod">The period for the upper channel.</param>
        /// <param name="lowerPeriod">The period for the lower channel</param>
        public DonchianChannel(string name, int upperPeriod, int lowerPeriod)
            : base(name)
        {
            WarmUpPeriod = 1 + Math.Max(upperPeriod, lowerPeriod);
            UpperBand = new Maximum(name + "_UpperBand", upperPeriod);
            LowerBand = new Minimum(name + "_LowerBand", lowerPeriod);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => UpperBand.IsReady && LowerBand.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator, which by convention is the mean value of the upper band and lower band.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (_previousInput != null)
            {
                UpperBand.Update(_previousInput.Time, _previousInput.High);
                LowerBand.Update(_previousInput.Time, _previousInput.Low);
            }

            _previousInput = input;
            return (UpperBand.Current.Value + LowerBand.Current.Value) / 2;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            UpperBand.Reset();
            LowerBand.Reset();
        }
    }
}