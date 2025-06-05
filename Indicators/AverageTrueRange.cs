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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The AverageTrueRange indicator is a measure of volatility introduced by Welles Wilder in his
    /// book: New Concepts in Technical Trading Systems. This indicator computes the TrueRange and then
    /// smoothes the TrueRange over a given period.
    ///
    /// TrueRange is defined as the maximum of the following:
    ///   High - Low
    ///   ABS(High - PreviousClose)
    ///   ABS(Low - PreviousClose)
    /// </summary>
    public class AverageTrueRange : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>This indicator is used to smooth the TrueRange computation</summary>
        /// <remarks>This is not exposed publicly since it is the same value as this indicator, meaning
        /// that this '_smoother' computers the ATR directly, so exposing it publicly would be duplication</remarks>
        private readonly IndicatorBase<IndicatorDataPoint> _smoother;

        /// <summary>
        /// Gets the true range which is the more volatile calculation to be smoothed by this indicator
        /// </summary>
        public IndicatorBase<IBaseDataBar> TrueRange { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _smoother.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Creates a new AverageTrueRange indicator using the specified period and moving average type
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The smoothing period used to smooth the true range values</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public AverageTrueRange(string name, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            WarmUpPeriod = period;
            TrueRange = new TrueRange(name);
            _smoother = movingAverageType.AsIndicator($"{name}_{movingAverageType}", period).Of(TrueRange, false);
        }

        /// <summary>
        /// Creates a new AverageTrueRange indicator using the specified period and moving average type
        /// </summary>
        /// <param name="period">The smoothing period used to smooth the true range values</param>
        /// <param name="movingAverageType">The type of smoothing used to smooth the true range values</param>
        public AverageTrueRange(int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"ATR({period})", period, movingAverageType)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            // compute the true range and then send it to our smoother
            TrueRange.Update(input);

            return _smoother.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _smoother.Reset();
            TrueRange.Reset();
            base.Reset();
        }
    }
}
