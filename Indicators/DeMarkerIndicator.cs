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
    /// In the DeMarker strategy, for some period of size N, set:
    /// <para>
    /// DeMax = High - Previous High, and 
    /// DeMin = Previous Low - Low
    /// </para>
    /// where, in the prior, if either term is less than zero (DeMax or DeMin), set it to zero.
    /// We can now define the indicator itself, DEM, as:
    ///<para>
    /// DEM = MA(DeMax)/(MA(DeMax)+MA(DeMin))
    ///</para>
    /// where MA denotes a Moving Average of period N.
    /// 
    /// https://www.investopedia.com/terms/d/demarkerindicator.asp
    /// </summary>
    public class DeMarkerIndicator : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly IndicatorBase<IndicatorDataPoint> _maxMA;
        private readonly IndicatorBase<IndicatorDataPoint> _minMA;
        private decimal _lastHigh;
        private decimal _lastLow;

        /// <summary>
        /// Initializes a new instance of the DeMarkerIndicator class with the specified period
        /// </summary>
        /// <param name="period">The period of the  DeMarker Indicator</param>
        /// <param name="type">The type of moving average to use in calculations</param>
        public DeMarkerIndicator(int period, MovingAverageType type = MovingAverageType.Simple)
            : this($"DEM({period},{type})", period, type)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DeMarkerIndicator class with the specified name and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the  DeMarker Indicator</param>
        /// <param name="type">The type of moving average to use in calculations</param>
        public DeMarkerIndicator(string name, int period, MovingAverageType type = MovingAverageType.Simple)
            : base(name)
        {
            _lastHigh = 0m;
            _lastLow = 0m;
            WarmUpPeriod = period;
            _maxMA = type.AsIndicator(period);
            _minMA = type.AsIndicator(period);
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _maxMA.IsReady && _minMA.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _lastHigh = 0m;
            _lastLow = 0m;
            _maxMA.Reset();
            _minMA.Reset();
            base.Reset();
        }


        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var deMax = 0m;
            var deMin = 0m;
            if (Samples > 1)
            {
                // By default, DeMin and DeMax must be 0m initially
                deMax = Math.Max(input.High - _lastHigh, 0);
                deMin = Math.Max(_lastLow - input.Low, 0);
            }

            _maxMA.Update(input.EndTime, deMax);
            _minMA.Update(input.EndTime, deMin);
            _lastHigh = input.High;
            _lastLow = input.Low;

            if (!IsReady)
            {
                return 0m;
            }

            var currentValue = _maxMA + _minMA;
            return currentValue > 0m ? _maxMA / currentValue : 0m;
        }
    }
}
