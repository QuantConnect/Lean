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
    /// Calculation of the Sortino Ratio, a modification of the <see cref="SharpeRatio"/>.
    ///
    /// Reference: https://www.cmegroup.com/education/files/rr-sortino-a-sharper-ratio.pdf
    /// Formula: S(x) = (R - T) / TDD
    /// Where:
    /// S(x) - Sortino ratio of x
    /// R - the average period return
    /// T - the target or required rate of return for the investment strategy under consideration. In
    /// Sortino’s early work, T was originally known as the minimum acceptable return, or MAR. In his
    /// more recent work, MAR is now referred to as the Desired Target Return.
    /// TDD - the target downside deviation. <see cref="TargetDownsideDeviation"/>
    /// </summary>
    public class SortinoRatio : SharpeRatio
    {
        /// <summary>
        /// Creates a new Sortino Ratio indicator using the specified periods
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">Period of historical observation for Sortino ratio calculation</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return for Sortino ratio calculation</param>
        public SortinoRatio(string name, int period, double minimumAcceptableReturn = 0)
            : base(name, period, minimumAcceptableReturn.SafeDecimalCast())
        {
            var denominator = new TargetDownsideDeviation(period, minimumAcceptableReturn).Of(
                RateOfChange
            );
            Ratio = Numerator.Over(denominator);
        }

        /// <summary>
        /// Creates a new SortinoRatio indicator using the specified periods
        /// </summary>
        /// <param name="period">Period of historical observation for Sortino ratio calculation</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return for Sortino ratio calculation</param>
        public SortinoRatio(int period, double minimumAcceptableReturn = 0)
            : this($"SORTINO({period},{minimumAcceptableReturn})", period, minimumAcceptableReturn)
        { }
    }
}
