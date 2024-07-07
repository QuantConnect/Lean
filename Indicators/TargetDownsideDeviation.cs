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
    /// This indicator computes the n-period target downside deviation. The target downside deviation is defined as the
    /// root-mean-square, or RMS, of the deviations of the realized return’s underperformance from the target return
    /// where all returns above the target return are treated as underperformance of 0.
    ///
    /// Reference: https://www.cmegroup.com/education/files/rr-sortino-a-sharper-ratio.pdf
    /// </summary>
    public class TargetDownsideDeviation
        : WindowIndicator<IndicatorDataPoint>,
            IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Minimum acceptable return (MAR) for target downside deviation calculation
        /// </summary>
        private readonly double _minimumAcceptableReturn;

        /// <summary>
        /// Initializes a new instance of the TargetDownsideDeviation class with the specified period and
        /// minimum acceptable return.
        ///
        /// The target downside deviation is defined as the root-mean-square, or RMS, of the deviations of
        /// the realized return’s underperformance from the target return where all returns above the target
        /// return are treated as underperformance of 0.
        /// </summary>
        /// <param name="period">The sample size of the target downside deviation</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return (MAR) for target downside deviation calculation</param>
        public TargetDownsideDeviation(int period, double minimumAcceptableReturn = 0)
            : this($"TDD({period},{minimumAcceptableReturn})", period, minimumAcceptableReturn)
        {
            _minimumAcceptableReturn = minimumAcceptableReturn;
        }

        /// <summary>
        /// Initializes a new instance of the TargetDownsideDeviation class with the specified period and
        /// minimum acceptable return.
        ///
        /// The target downside deviation is defined as the root-mean-square, or RMS, of the deviations of
        /// the realized return’s underperformance from the target return where all returns above the target
        /// return are treated as underperformance of 0.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The sample size of the target downside deviation</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return (MAR) for target downside deviation calculation</param>
        public TargetDownsideDeviation(string name, int period, double minimumAcceptableReturn = 0)
            : base(name, period) { }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="window">The window for the input history</param>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(
            IReadOnlyWindow<IndicatorDataPoint> window,
            IndicatorDataPoint input
        )
        {
            var avg = window
                .Select(x => Math.Pow(Math.Min(0, (double)x.Value - _minimumAcceptableReturn), 2))
                .Average();
            return Math.Sqrt(avg).SafeDecimalCast();
        }
    }
}
