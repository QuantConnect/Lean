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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Sharpe Ratio (SR) measures the relative expected return of a portfolio relative to its risk.
    /// That is, SR = (E(R) − r)/ σ where R is the portfolio's return, E denotes expectation, r is the risk-free rate,
    /// and σ is the standard deviation (risk) of the returns. This indicator in particular calculates the SR over
    /// a rolling window of data and is thus referred to as a "Rolling Sharpe Ratio" (RSR).
    /// </summary>
    public class RollingSharpeRatio : Indicator
    {
        readonly private StandardDeviation _risk;
        readonly private IndicatorBase<IndicatorDataPoint> _rollingMean;
        readonly private decimal _riskFreeRate;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _rollingMean.IsReady;

        /// <summary>
        /// Initializes a new instance of <see cref="RollingSharpeRatio"/>.
        /// </summary>
        /// <param name="period">The period over which to calculate the Sharpe ratio.</param>
        /// <param name="meanType">The (sample) mean method to use when estimating E(R).</param>
        /// <param name="riskFreeRate">The user-defined risk free return rate for the period.</param>
        public RollingSharpeRatio(int period, decimal riskFreeRate, MovingAverageType meanType)
            : this($"RSR({period},{riskFreeRate},{meanType})", period, riskFreeRate, meanType)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RollingSharpeRatio"/>.
        /// </summary>
        /// <param name="name">The name of this indicator.</param>
        /// <param name="period">The period over which to calculate the Sharpe ratio.</param>
        /// <param name="meanType">The (sample) mean method to use when estimating E(R).</param>
        /// <param name="riskFreeRate">The user-defined risk free return rate for the period.</param>
        public RollingSharpeRatio(
            string name,
            int period,
            decimal riskFreeRate = 0,
            MovingAverageType meanType = MovingAverageType.Simple
            )
            : base(name)
        {
            _rollingMean = meanType.AsIndicator(period);
            _risk = new StandardDeviation(period);
            _riskFreeRate = riskFreeRate;
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _rollingMean.Update(input);
            _risk.Update(input);
            return IsReady ? (_rollingMean - _riskFreeRate) / _risk : 0m;
        }
    }
}
