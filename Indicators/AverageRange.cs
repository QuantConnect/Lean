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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Represents the Average Range (AR) indicator, which calculates the average price range
    /// </summary>
    public class AverageRange : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// The Simple Moving Average (SMA) used to calculate the average of the price ranges.
        /// </summary>
        private readonly SimpleMovingAverage _sma;

        /// <summary>
        /// Initializes a new instance of the AverageRange class with the specified name and period.
        /// </summary>
        /// <param name="name">The name of the AR indicator.</param>
        /// <param name="period">The number of periods over which to compute the average range.</param>
        public AverageRange(string name, int period) : base(name)
        {
            _sma = new SimpleMovingAverage(name + "_SMA", period);
        }

        /// <summary>
        /// Initializes the AR indicator with the default name format and period.
        /// </summary>
        public AverageRange(int period)
            : this($"AR({period})", period)
        {
        }

        /// <summary>
        /// Indicates whether the indicator has enough data to start producing valid results.
        /// </summary>
        public override bool IsReady => _sma.IsReady;

        /// <summary>
        /// The number of periods needed to fully initialize the AR indicator.
        /// </summary>
        public int WarmUpPeriod => _sma.WarmUpPeriod;

        /// <summary>
        /// Resets the indicator and clears the internal state, including the SMA.
        /// </summary>
        public override void Reset()
        {
            _sma.Reset();
            base.Reset();
        }

        /// <summary>
        /// Computes the next value of the Average Range (AR) by calculating the price range (high - low)
        /// and passing it to the SMA to get the smoothed value.
        /// </summary>
        /// <param name="input">The input data for the current bar, including open, high, low, close values.</param>
        /// <returns>The computed AR value, which is the smoothed average of price ranges.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var priceRange = input.High - input.Low;
            // Update the SMA with the price range
            _sma.Update(new IndicatorDataPoint(input.EndTime, priceRange));
            return _sma.Current.Value;
        }
    }
}