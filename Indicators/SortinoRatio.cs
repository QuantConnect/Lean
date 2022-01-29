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
    public class SortinoRatio : SharpeRatio
    {
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
            : base(name, period, riskFreeRate)
        {
            // calculate sortino ratio using indicators
            _roc = new RateOfChange(1);
            var std = new StandardDownsideDeviation(period, riskFreeRate).Of(_roc);
            var sma = _roc.SMA(period);
            _sharpeRatio = sma.Minus(riskFreeRate).Over(std);
        }
    }
}
