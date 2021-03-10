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
    /// Calculation of the Sharpe Ratio (SR) developed by William F. Sharpe.
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
        /// Counter for storing the warmup period prior to determining the indicator is ready
        /// </summary>
        private int _counter;
        
        /// <summary>
        /// RateOfChange indicator for calculating the sharpe ratio
        /// </summary>
        private RateOfChange _roc;

        /// <summary>
        /// Indicator to store the calculation of the sharpe ratio
        /// </summary>
        private CompositeIndicator<IndicatorDataPoint> _sharpeRatio;
        
        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Returns whether the indicator is properly initalized with data
        /// </summary>
        public override bool IsReady => _counter == WarmUpPeriod && _sharpeRatio.IsReady && _roc.IsReady;

        /// <summary>
        /// Creates a new SharpeRatio indicator using the specified periods
        /// </summary>
		/// <param name="name">The name of this indicator</param>
		/// <param name="sharpePeriod">Period of historical observation for sharpe ratio calculation</param>
		/// <param name="riskFreeRate">Risk-free rate for sharpe ratio calculation</param>
        public SharpeRatio(string name, int sharpePeriod, decimal riskFreeRate = 0.0m)
            : base(name)
        {
            // set counter to 0
            _counter = 0;

            // calculate sharpe ratio using indicators
            _roc = new RateOfChange(1);
            var std = new StandardDeviation(sharpePeriod).Of(_roc);
            var sma = _roc.SMA(sharpePeriod);
            _sharpeRatio = sma.Minus(riskFreeRate).Over(std);

            // define warmup value
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
            // increments counter until warmup period has been successfully reached
            if (_counter < WarmUpPeriod)
                _counter++;

            // update indicators
            _roc.Update(input);
            return _sharpeRatio;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _counter = 0;
            _sharpeRatio.Reset();
            _roc.Reset();
            base.Reset();
        }
    }
}