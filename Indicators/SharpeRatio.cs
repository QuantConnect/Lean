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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Calculation of the Sharpe Ratio (SR) developed by William F. Sharpe.
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
        /// Length of lookback period for the Sharpe ratio calculation
        /// </summary>
        private readonly int _period;

        /// <summary>
        /// RateOfChange indicator for calculating the sharpe ratio
        /// </summary>
        protected RateOfChange RateOfChange { get; }

        /// <summary>
        /// Indicator to store the calculation of the sharpe ratio
        /// </summary>
        protected IndicatorBase Ratio { get; set;  }

        /// <summary>
        /// Indicator to store the numerator of the Sharpe ratio calculation
        /// </summary>
        protected IndicatorBase Numerator { get; }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Returns whether the indicator is properly initialized with data
        /// </summary>
        public override bool IsReady => Ratio.Samples > _period;

        /// <summary>
        /// Creates a new Sharpe Ratio indicator using the specified periods
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">Period of historical observation for sharpe ratio calculation</param>
        /// <param name="riskFreeRate">Risk-free rate for sharpe ratio calculation</param>
        public SharpeRatio(string name, int period, decimal riskFreeRate = 0.0m)
            : base(name)
        {
            _period = period;

            // calculate sharpe ratio using indicators
            RateOfChange = new RateOfChange(1);
            Numerator = RateOfChange.SMA(period).Minus(riskFreeRate);
            var denominator = new StandardDeviation(period).Of(RateOfChange);
            Ratio = Numerator.Over(denominator);

            // define warmup value; 
            // _roc is the base of our indicator chain + period of STD and SMA
            WarmUpPeriod = RateOfChange.WarmUpPeriod + period;
        }

        /// <summary>
        /// Creates a new SharpeRatio indicator using the specified periods
        /// </summary>
        /// <param name="period">Period of historical observation for sharpe ratio calculation</param>
        /// <param name="riskFreeRate">Risk-free rate for sharpe ratio calculation</param>
        public SharpeRatio(int period, decimal riskFreeRate = 0.0m)
            : this($"SR({period},{riskFreeRate})", period, riskFreeRate)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            RateOfChange.Update(input);
            return Ratio;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            Ratio.Reset();
            RateOfChange.Reset();
            base.Reset();
        }
    }
}
