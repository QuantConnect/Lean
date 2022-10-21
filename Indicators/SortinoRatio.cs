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
using System.Linq;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Calculation of the Sortino Ratio, a modification of the Sharpe Ratio.
    ///
    /// Reference: https://www.investopedia.com/terms/s/sortinoratio.asp
    /// Formula: S(x) = (mean(Rx) - Rf) / stdDev(Rx_d)
    /// Where:
    /// S(x) - Sortino ratio of x
    /// Rx - trailing returns of x
    /// Rf - risk-free rate
    /// Rx_d - trailing downside returns of x
    /// </summary>
    public class SortinoRatio : SharpeRatio
    {
        /// <summary>
        /// Creates a new Sortino Ratio indicator using the specified periods
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">Period of historical observation for Sortino ratio calculation</param>
        /// <param name="riskFreeRate">Risk-free rate for Sortino ratio calculation</param>
        public SortinoRatio(string name, int period, decimal riskFreeRate = 0.0m)
            : base(name, period, riskFreeRate)
        {
        }
        
        /// <summary>
        /// Creates a new SortinoRatio indicator using the specified periods
        /// </summary>
        /// <param name="period">Period of historical observation for Sortino ratio calculation</param>
        /// <param name="riskFreeRate">Risk-free rate for Sortino ratio calculation</param>
        public SortinoRatio(int period, decimal riskFreeRate = 0.0m)
            : this($"SORTINO({period},{riskFreeRate})", period, riskFreeRate)
        {
        }

        /// <summary>
        /// Create the denominator of the Sortino Ratio equation
        /// </summary>
        /// <param name="roc">The denominator is a function of the returns</param>
        /// <returns>An Indicator object representing the denominator</returns>
        protected override IndicatorBase CreateStandardDeviation(RateOfChange roc)
        {
            return new StandardDownsideDeviation(_period).Of(roc);
        }
    }
}
