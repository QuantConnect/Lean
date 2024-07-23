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
    /// This indicator computes the Rogers-Satchell Volatility
    /// It is an estimator for measuring the volatility of securities 
    /// with an average return not equal to zero.
    /// </summary>
    public class RogersSatchellVolatility : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
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
        /// Initializes a new instance of the <see cref="RogersSatchellVolatility"/> class using the specified parameters
        /// </summary> 
        /// <param name="period">The period of moving window</param>
        public RogersSatchellVolatility(int period)
            : this($"RSV({period})", period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RogersSatchellVolatility"/> class using the specified parameters
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of moving window</param>
        public RogersSatchellVolatility(string name, int period)
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
            if ((input.Open == 0) || (input.High == 0) || (input.Low == 0) || (input.Close == 0))
            {
                // return a sentinel value
                return decimal.MinValue;
            }

            _rollingSum.Update(input.EndTime, (decimal)
                (Math.Log((double)input.High / (double)input.Close) * Math.Log((double)input.High / (double)input.Open)
                + Math.Log((double)input.Low / (double)input.Close) * Math.Log((double)input.Low / (double)input.Open))
            );

            if (IsReady)
            {
                return (decimal)Math.Sqrt(((double)_rollingSum.Current.Value) / _period);
            }
            else
            {
                return 0m;
            }
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
