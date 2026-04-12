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
    /// Represents the Yang Zhang volatility estimator.
    /// Yang Zhang is the most efficient known estimator of historical volatility that
    /// is independent of drift and opening gaps. It combines overnight (open-to-previous-close)
    /// variance, intraday (close-to-open) variance, and Rogers-Satchell variance with an
    /// optimal weighting factor k.
    /// Reference: Yang, D. and Zhang, Q. (2000). "Drift Independent Volatility Estimation
    /// Based on High, Low, Open, and Close Prices."
    /// </summary>
    public class YangZhangVolatility : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;
        private readonly decimal _k;

        private decimal _previousClose;

        private readonly IndicatorBase<IndicatorDataPoint> _overnightSum;
        private readonly IndicatorBase<IndicatorDataPoint> _overnightSumSq;
        private readonly IndicatorBase<IndicatorDataPoint> _intradaySum;
        private readonly IndicatorBase<IndicatorDataPoint> _intradaySumSq;
        private readonly IndicatorBase<IndicatorDataPoint> _rsSum;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period + 1;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period + 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="YangZhangVolatility"/> class using the specified name and period.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the rolling window</param>
        public YangZhangVolatility(string name, int period)
            : base(name)
        {
            if (period < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(period),
                    "YangZhangVolatility requires a period of at least 2.");
            }

            _period = period;

            // k minimizes the variance of the combined estimator
            _k = 0.34m / (1.34m + (period + 1m) / (period - 1m));

            _overnightSum = new Sum(name + "_OvernightSum", period);
            _overnightSumSq = new Sum(name + "_OvernightSumSq", period);
            _intradaySum = new Sum(name + "_IntradaySum", period);
            _intradaySumSq = new Sum(name + "_IntradaySumSq", period);
            _rsSum = new Sum(name + "_RSSum", period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YangZhangVolatility"/> class using the specified period.
        /// </summary>
        /// <param name="period">The period of the rolling window</param>
        public YangZhangVolatility(int period)
            : this($"YZV({period})", period)
        {
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (input.Open <= 0 || input.High <= 0 || input.Low <= 0 || input.Close <= 0)
            {
                return 0m;
            }

            if (Samples == 1)
            {
                _previousClose = input.Close;
                return 0m;
            }

            if (_previousClose <= 0)
            {
                _previousClose = input.Close;
                return 0m;
            }

            // Overnight return: ln(Open / PreviousClose)
            var overnightReturn = (decimal)Math.Log((double)(input.Open / _previousClose));

            // Intraday return: ln(Close / Open)
            var intradayReturn = (decimal)Math.Log((double)(input.Close / input.Open));

            // Rogers-Satchell per-bar value
            var rsValue = (decimal)(
                Math.Log((double)input.High / (double)input.Close) * Math.Log((double)input.High / (double)input.Open)
                + Math.Log((double)input.Low / (double)input.Close) * Math.Log((double)input.Low / (double)input.Open));

            // Feed rolling sums
            _overnightSum.Update(input.EndTime, overnightReturn);
            _overnightSumSq.Update(input.EndTime, overnightReturn * overnightReturn);
            _intradaySum.Update(input.EndTime, intradayReturn);
            _intradaySumSq.Update(input.EndTime, intradayReturn * intradayReturn);
            _rsSum.Update(input.EndTime, rsValue);

            _previousClose = input.Close;

            if (!IsReady)
            {
                return 0m;
            }

            var n = (decimal)_period;

            // Overnight variance: sample variance of overnight returns
            var overnightVariance = (_overnightSumSq.Current.Value - _overnightSum.Current.Value * _overnightSum.Current.Value / n) / (n - 1m);

            // Intraday variance: sample variance of intraday returns
            var intradayVariance = (_intradaySumSq.Current.Value - _intradaySum.Current.Value * _intradaySum.Current.Value / n) / (n - 1m);

            // Rogers-Satchell variance: population mean of RS values
            var rsVariance = _rsSum.Current.Value / n;

            // Yang Zhang combined estimator
            var yzVariance = overnightVariance + _k * intradayVariance + (1m - _k) * rsVariance;

            return (decimal)Math.Sqrt((double)Math.Max(0m, yzVariance));
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _previousClose = 0;
            _overnightSum.Reset();
            _overnightSumSq.Reset();
            _intradaySum.Reset();
            _intradaySumSq.Reset();
            _rsSum.Reset();
            base.Reset();
        }
    }
}
