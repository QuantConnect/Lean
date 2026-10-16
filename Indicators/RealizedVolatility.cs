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
    /// This indicator computes the realized volatility of a security using the
    /// Parkinson high-low range estimator.
    /// Unlike close-to-close estimators, it uses each bar's High/Low range, which
    /// converges faster but assumes no drift within the bar and no overnight gaps.
    /// </summary>
    public class RealizedVolatility : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private static readonly double Ln2Times4 = 4.0 * Math.Log(2.0);

        private readonly int _period;
        private readonly IndicatorBase<IndicatorDataPoint> _rollingSum;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= _period;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod => _period;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealizedVolatility"/> class using the specified parameters
        /// </summary>
        /// <param name="period">The period of moving window</param>
        public RealizedVolatility(int period)
            : this($"RV({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RealizedVolatility"/> class using the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of moving window</param>
        public RealizedVolatility(string name, int period)
            : base(name)
        {
            _period = period;
            _rollingSum = new Sum(name + "_Sum", period);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (input.High <= input.Low || input.Low <= 0)
            {
                // return a sentinel value
                return 0m;
            }

            var term = Math.Pow(Math.Log((double)input.High / (double)input.Low), 2) / Ln2Times4;
            _rollingSum.Update(input.EndTime, (decimal)term);

            if (IsReady)
            {
                return (decimal)Math.Sqrt((double)_rollingSum.Current.Value / _period);
            }

            return 0m;
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
