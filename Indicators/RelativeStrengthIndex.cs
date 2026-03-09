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
    /// Represents the  Relative Strength Index (RSI) developed by K. Welles Wilder.
    /// You can optionally specified a different moving average type to be used in the computation
    /// </summary>
    public class RelativeStrengthIndex : Indicator, IIndicatorWarmUpPeriodProvider
    {
        private IndicatorDataPoint _previousInput;

        /// <summary>
        /// Gets the type of indicator used to compute AverageGain and AverageLoss
        /// </summary>
        public MovingAverageType MovingAverageType { get; }

        /// <summary>
        /// Gets the EMA for the down days
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> AverageLoss { get; }

        /// <summary>
        /// Gets the indicator for average gain
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> AverageGain { get; }

        /// <summary>
        /// Initializes a new instance of the RelativeStrengthIndex class with the specified name and period
        /// </summary>
        /// <param name="period">The period used for up and down days</param>
        /// <param name="movingAverageType">The type of moving average to be used for computing the average gain/loss values</param>
        public RelativeStrengthIndex(int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : this($"RSI({period})", period, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RelativeStrengthIndex class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period used for up and down days</param>
        /// <param name="movingAverageType">The type of moving average to be used for computing the average gain/loss values</param>
        public RelativeStrengthIndex(string name, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders)
            : base(name)
        {
            MovingAverageType = movingAverageType;
            AverageGain = movingAverageType.AsIndicator(name + "Up", period);
            AverageLoss = movingAverageType.AsIndicator(name + "Down", period);
            WarmUpPeriod = period + 1;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => AverageGain.IsReady && AverageLoss.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            if (_previousInput != null && input.Value >= _previousInput.Value)
            {
                AverageGain.Update(input.EndTime, input.Value - _previousInput.Value);
                AverageLoss.Update(input.EndTime, 0m);
            }
            else if (_previousInput != null && input.Value < _previousInput.Value)
            {
                AverageGain.Update(input.EndTime, 0m);
                AverageLoss.Update(input.EndTime, _previousInput.Value - input.Value);
            }

            _previousInput = input;

            // make sure the difference averages are not negative
            // (can happen with some types of moving averages -- e.g. DEMA)
            var averageLoss = AverageLoss < 0 ? 0 : AverageLoss.Current.Value;
            var averageGain = AverageGain < 0 ? 0 : AverageGain.Current.Value;

            // Round AverageLoss to avoid computing RSI with very small numbers that lead to overflow exception on the division operation below
            if (Math.Round(averageLoss, 10) == 0m)
            {
                // all up days is 100
                return 100m;
            }

            var rs = averageGain / averageLoss;

            return 100m - 100m / (1 + rs);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _previousInput = null;
            AverageGain.Reset();
            AverageLoss.Reset();
            base.Reset();
        }
    }
}
