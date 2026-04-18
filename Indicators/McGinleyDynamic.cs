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
    /// Represents the McGinley Dynamic (MGD)
    /// It is a type of moving average that was designed to track the market better 
    /// than existing moving average indicators.
    /// It is a technical indicator that improves upon moving average lines by adjusting 
    /// for shifts in market speed.
    /// </summary>
    public class McGinleyDynamic : WindowIndicator<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// A rolling sum for computing the average for the given period
        /// </summary>
        private readonly IndicatorBase<IndicatorDataPoint> _rollingSum;

        private readonly int _period;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _rollingSum.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public override int WarmUpPeriod => Period;

        /// <summary>
        /// Initializes a new instance of the McGinleyDynamic class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the McGinley Dynamic</param>
        public McGinleyDynamic(string name, int period)
            : base(name, period)
        {
            if (period == 0) throw new ArgumentException("Period can not be zero");
            _period = period;
            _rollingSum = new Sum(name + "_Sum", period);
        }

        /// <summary>
        /// Initializes a new instance of the McGinleyDynamic class with the default name and period
        /// </summary>
        /// <param name="period">The period of the McGinley Dynamic</param>
        public McGinleyDynamic(int period)
            : this($"MGD({period})", period)
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
            _rollingSum.Update(input.EndTime, input.Value);
            if (!IsReady)
            {
                return 0;
            }

            if (Samples == _period)
            {
                return _rollingSum.Current.Value / _period;
            }

            if (Current.Value == 0 || input.Value == 0)
            {
                return Current.Value;
            }

            var ratioValue = (double)input.Value.SafeDivision(Current.Value, 0);
            if (ratioValue == 0) return Current.Value;
            var denominator = _period * (decimal)Math.Pow(ratioValue, 4.0);
            return Current.Value + (input.Value - Current.Value).SafeDivision(denominator, 0);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _rollingSum.Reset();
            base.Reset();
        }
    }
}
