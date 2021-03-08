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
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Calculation of the rolling moving average for the Sharpe Ratio (sr) developed by William F. Sharpe
    /// You can optionally specify a different moving average type to be used in the computation.
    ///
    /// Reference: https://www.investopedia.com/articles/07/sharpe_ratio.asp
    /// Formula: S(x) = (Rx - Rf) / stdDev(Rx)
    /// Where:
    /// S(x) - sharpe ratio of x
    /// Rx - average rate of return for x
    /// Rf - risk-free rate
    /// </summary>
    public class SharpeRatio : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// RollingWindow for storing historical values for percent change calculation
        /// </summary>
        private RollingWindow<IndicatorDataPoint> _values;

        /// <summary>
        /// StandardDeviation indicator for calculating the standard deviation over period
        /// </summary>
        private StandardDeviation _std;

        /// <summary>
        /// Stores the period of the historical percent changes used for calculating sharpe ratio
        /// </summary>
        public int SharpePeriod { get; }

        /// <summary>
        /// Stores the value of the risk-free rate of return for calculation
        /// </summary>
        public decimal RiskFreeRate { get; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Returns whether the indicator is properly initalized with data
        /// </summary>
        public override bool IsReady => _values.IsReady && _std.IsReady;

        /// <summary>
        /// Creates a new SharpeRatio indicator using the specified periods
        /// </summary>
		/// <param name="name">The name of this indicator</param>
		/// <param name="sharpePeriod">Period of historical observation for sharpe ratio calculation</param>
		/// <param name="riskFreeRate">Risk-free rate for sharpe ratio calculation</param>
        public SharpeRatio(string name, int sharpePeriod, decimal riskFreeRate = 0.0m)
            : base(name)
        {
            // init private variables
            _std = new StandardDeviation(sharpePeriod);
            _values = new RollingWindow<IndicatorDataPoint>(sharpePeriod + 1);

            // init public variables
            SharpePeriod = sharpePeriod;
            RiskFreeRate = riskFreeRate;
            WarmUpPeriod = sharpePeriod + 1;
        }

        /// <summary>
        /// Creates a new SharpeRatio indicator using the specified periods
        /// </summary>
        /// <param name="sharpePeriod">Period of historical observation for sharpe ratio calculation</param>
		/// <param name="riskFreeRate">Risk-free rate for sharpe ratio calculation</param>
        public SharpeRatio(int sharpePeriod, decimal riskFreeRate = 0.0m)
            : this($"SR({sharpePeriod})", sharpePeriod, riskFreeRate)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
		/// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            // update indicators
            _values.Add(input);
            _std.Update(input);

            // check for IsReady
            if (!IsReady)
                return 0.0m;

            // calculates SharpeRatio
            // makes sure no divisibilty errors occur
            decimal pc = input.Value != 0.0m ? ((input.Value - _values[SharpePeriod].Value) / input.Value) : 0.0m;
            decimal stdAsPercentage = input.Value != 0.0m ? _std / input.Value : 0.0m;
            var sharpe = stdAsPercentage != 0.0m ? (pc - RiskFreeRate) / stdAsPercentage : 0.0m;
            return sharpe;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _std.Reset();
            _values.Reset();
            base.Reset();
        }
    }
}