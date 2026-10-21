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
    /// Calculation of the Rachev Ratio, the expected tail return of the best outcomes
    /// divided by the expected tail loss of the worst outcomes of the return distribution.
    ///
    /// Reference: Biglova, Ortobelli, Rachev and Stoyanov (2004), "Different Approaches to Risk Estimation
    /// in Portfolio Theory", The Journal of Portfolio Management 31(1), 103-112.
    /// https://en.wikipedia.org/wiki/Rachev_ratio
    /// Formula: RR(x) = ETL_alpha(Rf - Rx) / ETL_beta(Rx - Rf)
    /// Where:
    /// RR(x) - Rachev ratio of x
    /// Rx - one period returns of x over the lookback period
    /// Rf - risk-free rate per period
    /// ETL_q(X) - expected tail loss (CVaR) of X in the worst q fraction of the observations
    ///
    /// The historical expected tail loss weights the observation at the tail boundary by its fraction,
    /// as in Acerbi and Tasche (2002), "On the coherence of expected shortfall".
    /// </summary>
    public class RachevRatio : IndicatorBase<IndicatorDataPoint>, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// Tail probability of the best returns, used in the numerator
        /// </summary>
        private readonly double _alpha;

        /// <summary>
        /// Tail probability of the worst returns, used in the denominator
        /// </summary>
        private readonly double _beta;

        /// <summary>
        /// Risk-free rate per period
        /// </summary>
        private readonly double _riskFreeRate;

        /// <summary>
        /// RateOfChange indicator to calculate the returns
        /// </summary>
        private readonly RateOfChange _rateOfChange;

        /// <summary>
        /// Rolling window to store the excess returns of the input data
        /// </summary>
        private readonly RollingWindow<double> _returns;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _returns.IsReady;

        /// <summary>
        /// Creates a new RachevRatio indicator using the specified periods
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">Number of returns in the lookback period</param>
        /// <param name="alpha">Tail probability of the best returns, the numerator of the ratio</param>
        /// <param name="beta">Tail probability of the worst returns, the denominator of the ratio</param>
        /// <param name="riskFreeRate">Risk-free rate per period, subtracted from each return</param>
        public RachevRatio(string name, int period, double alpha = 0.05, double beta = 0.05, double riskFreeRate = 0)
            : base(name)
        {
            if (period < 2)
            {
                throw new ArgumentException($"Period parameter for RachevRatio indicator must be greater than 1 but was {period}");
            }
            if (alpha <= 0 || alpha >= 1)
            {
                throw new ArgumentException($"Alpha parameter for RachevRatio indicator must be between 0 and 1 but was {alpha}");
            }
            if (beta <= 0 || beta >= 1)
            {
                throw new ArgumentException($"Beta parameter for RachevRatio indicator must be between 0 and 1 but was {beta}");
            }

            _alpha = alpha;
            _beta = beta;
            _riskFreeRate = riskFreeRate;
            _rateOfChange = new RateOfChange(1);
            _returns = new RollingWindow<double>(period);
            WarmUpPeriod = period + 1;
        }

        /// <summary>
        /// Creates a new RachevRatio indicator using the specified periods
        /// </summary>
        /// <param name="period">Number of returns in the lookback period</param>
        /// <param name="alpha">Tail probability of the best returns, the numerator of the ratio</param>
        /// <param name="beta">Tail probability of the worst returns, the denominator of the ratio</param>
        /// <param name="riskFreeRate">Risk-free rate per period, subtracted from each return</param>
        public RachevRatio(int period, double alpha = 0.05, double beta = 0.05, double riskFreeRate = 0)
            : this($"RACHEV({period},{alpha},{beta},{riskFreeRate})", period, alpha, beta, riskFreeRate)
        {
        }

        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _rateOfChange.Update(input);
            if (!_rateOfChange.IsReady)
            {
                return 0m;
            }

            _returns.Add((double)_rateOfChange.Current.Value - _riskFreeRate);
            if (!_returns.IsReady)
            {
                return 0m;
            }

            var sorted = _returns.OrderBy(x => x).ToArray();
            var expectedTailLoss = -TailMean(sorted, _beta);
            if (expectedTailLoss == 0)
            {
                return 0m;
            }

            Array.Reverse(sorted);
            var expectedTailReturn = TailMean(sorted, _alpha);
            return (expectedTailReturn / expectedTailLoss).SafeDecimalCast();
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _rateOfChange.Reset();
            _returns.Reset();
            base.Reset();
        }

        /// <summary>
        /// Average of the first <paramref name="probability"/> fraction of the given values.
        /// When the tail size is not a whole number of observations, the last observation
        /// in the tail is weighted by its fractional part.
        /// </summary>
        /// <param name="values">The sorted values, starting with the tail</param>
        /// <param name="probability">The fraction of the values in the tail</param>
        /// <returns>The average of the tail</returns>
        private static double TailMean(double[] values, double probability)
        {
            var tailSize = probability * values.Length;
            var wholeObservations = (int)Math.Floor(tailSize);

            var sum = 0d;
            for (var i = 0; i < wholeObservations; i++)
            {
                sum += values[i];
            }
            if (wholeObservations < values.Length)
            {
                sum += (tailSize - wholeObservations) * values[wholeObservations];
            }
            return sum / tailSize;
        }
    }
}
