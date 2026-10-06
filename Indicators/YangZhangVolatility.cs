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
    /// This indicator computes the Yang Zhang Volatility, a drift independent estimator combining the
    /// overnight, intraday and Rogers-Satchell variances.
    /// Reference: https://portfolioslab.com/tools/yang-zhang
    /// </summary>
    public class YangZhangVolatility : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly IndicatorBase<IndicatorDataPoint> _sumOvernight;
        private readonly IndicatorBase<IndicatorDataPoint> _sumOvernightSquared;
        private readonly IndicatorBase<IndicatorDataPoint> _sumIntraday;
        private readonly IndicatorBase<IndicatorDataPoint> _sumIntradaySquared;
        private readonly IndicatorBase<IndicatorDataPoint> _sumRogersSatchell;
        private decimal _previousClose;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _sumOvernight.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// Includes the close before the window, needed by the first overnight return
        /// </summary>
        public int WarmUpPeriod => _period + 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="YangZhangVolatility"/> class using the specified parameters
        /// </summary>
        /// <param name="period">The number of bars used to estimate volatility</param>
        public YangZhangVolatility(int period)
            : this($"YZV({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YangZhangVolatility"/> class using the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The number of bars used to estimate volatility</param>
        public YangZhangVolatility(string name, int period)
            : base(name)
        {
            if (period < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(period), period, "The period must be at least 2.");
            }

            _period = period;
            _sumOvernight = new Sum(name + "_OvernightSum", period);
            _sumOvernightSquared = new Sum(name + "_OvernightSumSquared", period);
            _sumIntraday = new Sum(name + "_IntradaySum", period);
            _sumIntradaySquared = new Sum(name + "_IntradaySumSquared", period);
            _sumRogersSatchell = new Sum(name + "_RogersSatchellSum", period);
            _previousClose = 0m;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var previousClose = _previousClose;
            _previousClose = input.Close;

            if ((previousClose == 0) || (input.Open == 0) || (input.High == 0) || (input.Low == 0) || (input.Close == 0))
            {
                // first bar, or a zero price: logarithms are undefined
                return 0m;
            }

            var overnight = (decimal)Math.Log((double)(input.Open / previousClose));
            var intraday = (decimal)Math.Log((double)(input.Close / input.Open));
            var rogersSatchell = (decimal)
                (Math.Log((double)input.High / (double)input.Close) * Math.Log((double)input.High / (double)input.Open)
                + Math.Log((double)input.Low / (double)input.Close) * Math.Log((double)input.Low / (double)input.Open));

            _sumOvernight.Update(input.EndTime, overnight);
            _sumOvernightSquared.Update(input.EndTime, overnight * overnight);
            _sumIntraday.Update(input.EndTime, intraday);
            _sumIntradaySquared.Update(input.EndTime, intraday * intraday);
            _sumRogersSatchell.Update(input.EndTime, rogersSatchell);

            if (!IsReady)
            {
                return 0m;
            }

            var n = (double)_period;
            var sumOvernight = (double)_sumOvernight.Current.Value;
            var sumOvernightSquared = (double)_sumOvernightSquared.Current.Value;
            var sumIntraday = (double)_sumIntraday.Current.Value;
            var sumIntradaySquared = (double)_sumIntradaySquared.Current.Value;

            // subtracting the squared sum from the sum of squares can cancel out to a small
            // negative value, which the square root would turn into a NaN
            var overnightVariance = Math.Max(0d, (sumOvernightSquared - sumOvernight * sumOvernight / n) / (n - 1d));
            var intradayVariance = Math.Max(0d, (sumIntradaySquared - sumIntraday * sumIntraday / n) / (n - 1d));
            var rogersSatchellVariance = (double)_sumRogersSatchell.Current.Value / n;

            var weight = 0.34 / (1.34 + (n + 1d) / (n - 1d));

            return (decimal)Math.Sqrt(overnightVariance + (weight * intradayVariance) + ((1d - weight) * rogersSatchellVariance));
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _sumOvernight.Reset();
            _sumOvernightSquared.Reset();
            _sumIntraday.Reset();
            _sumIntradaySquared.Reset();
            _sumRogersSatchell.Reset();
            _previousClose = 0m;
            base.Reset();
        }
    }
}
