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
    /// Calculation of the Sortino Ratio (SR) named after Frank A. Sortino.
    ///
    /// Reference: https://www.investopedia.com/terms/s/sortinoratio.asp
    /// Formula: S(x) = (Rx - Rf) / stdDev(d)
    /// Where:
    /// S(x) - sortino ratio of x
    /// Rx - actual or expected portfolio return for x
    /// Rf - risk-free rate
    /// d - downside portfolio risk
    /// </summary>
    public class SortinoRatio : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        private readonly int _period;

        /// <summary>
        /// RateOfChange indicator for calculating sma for the sortino ratio
        /// </summary>
        private readonly RateOfChange _rocForSMA;

        /// <summary>
        /// RateOfChange indicator for calculating downside deviation for the sortino ratio
        /// </summary>
        private readonly RateOfChange _rocForSTD;

        /// <summary>
        /// Indicator to store the calculation of the sortino ratio
        /// </summary>
        private readonly CompositeIndicator _sortinoRatio;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Returns whether the indicator is properly initialized with data
        /// </summary>
        public override bool IsReady => _sortinoRatio.Samples > _period;

        /// <summary>
        /// Creates a new Sortino Ratio indicator using the specified periods
        /// </summary>
        /// <param name="period">Period of historical observation for sortino ratio calculation</param>
        /// <param name="riskFreeRate">Risk-free rate for sortino ratio calculation</param>
        public SortinoRatio(int period, decimal riskFreeRate = 0.0m)
            : this($"SORTINO({period},{riskFreeRate})", period, riskFreeRate)
        {
        }

        /// <summary>
        /// Creates a new Sortino Ratio indicator using the specified periods
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">Period of historical observation for sortino ratio calculation</param>
        /// <param name="riskFreeRate">Risk-free rate for sortino ratio calculation</param>
        public SortinoRatio(string name, int period, decimal riskFreeRate = 0.0m)
            : base(name)
        {
            _period = period;

            // calculate sortino ratio using indicators
            _rocForSMA = new RateOfChange(1);
            _rocForSTD = new RateOfChange(1);
            _rocForSTD.Updated += (sender, args) =>
            {                
                //ensure we are only using the downside deviation for calculation
                var _roc = sender.ConvertInvariant<RateOfChange>();
                if (_roc.Current.Value > 0)
                    _roc.Current.Value = 0;
            };
            var std = new StandardDeviation(period).Of(_rocForSTD);
            var sma = _rocForSMA.SMA(period);
            _sortinoRatio = sma.Minus(riskFreeRate).Over(std);

            // define warmup value; 
            // _roc is the base of our indicator chain + period of STD and SMA
            WarmUpPeriod = _rocForSMA.WarmUpPeriod + _period;
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _rocForSMA.Update(input);
            _rocForSTD.Update(input);
            return _sortinoRatio;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _sortinoRatio.Reset();
            _rocForSMA.Reset();
            _rocForSTD.Reset();
            base.Reset();
        }
    }
}
