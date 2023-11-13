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
using QuantConnect.Data.Market;
using MathNet.Numerics.Statistics;
using System.Linq;
using MathNet.Numerics.Distributions;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// In financial analysis, the Alpha indicator is used to measure the performance of an investment (such as a stock or ETF) 
    /// relative to a benchmark index, often representing the broader market. Alpha indicates the excess return of the investment 
    /// compared to the return of the benchmark index. A positive Alpha implies that the investment has performed better than 
    /// its benchmark index, while a negative Alpha indicates underperformance.
    /// 
    /// The S&P 500 index is frequently used as a benchmark in Alpha calculations to represent the overall market performance. 
    /// Alpha is an essential tool for investors to assess the active return on an investment and understand how well their 
    /// investment is performing compared to the market average.
    /// </summary>

    public class Alpha : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        /// <summary>
        /// RollingWindow to store the data points of the target symbol
        /// </summary>
        private readonly RollingWindow<decimal> _targetDataPoints;

        /// <summary>
        /// RollingWindow to store the data points of the reference symbol
        /// </summary>
        private readonly RollingWindow<decimal> _referenceDataPoints;

        /// <summary>
        /// Symbol of the reference used
        /// </summary>
        private readonly Symbol _referenceSymbol;

        /// <summary>
        /// Symbol of the target used
        /// </summary>
        private readonly Symbol _targetSymbol;

        /// <summary>
        /// RollingWindow of returns of the target symbol in the given period for the alpha
        /// </summary>
        private readonly RollingWindow<double> _targetAlphaReturns;

        /// <summary>
        /// RollingWindow of returns of the reference symbol in the given period for the alpha
        /// </summary>
        private readonly RollingWindow<double> _referenceAlphaReturns;

        /// <summary>
        /// RollingWindow of returns of the target symbol in the given period for the beta
        /// </summary>
        private readonly RollingWindow<double> _targetBetaReturns;

        /// <summary>
        /// RollingWindow of returns of the reference symbol in the given period for the beta
        /// </summary>
        private readonly RollingWindow<double> _referenceBetaReturns;

        /// <summary>
        /// Alpha of the target used in relation with the reference
        /// </summary>
        private decimal _alpha;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetDataPoints.Samples >= WarmUpPeriod && _referenceDataPoints.Samples >= WarmUpPeriod;

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="alphaPeriod">The Alpha period of this indicator</param>
        /// <param name="betaPeriod">The Beta period of this indicator</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod)
            : base(name)
        {
            // Assert that the target and reference symbols are not the same
            if (targetSymbol == referenceSymbol)
            {
                throw new ArgumentException("The target and reference symbols cannot be the same.");
            }

            // Assert that the period is greater than 2, otherwise the alpha can not be computed
            if (alphaPeriod < 2)
            {
                throw new ArgumentException("The period must be greater than 2.");
            }

            // Assert that the beta period is greater than 2, otherwise the beta can not be computed
            if (betaPeriod < 2)
            {
                throw new ArgumentException("The beta period must be greater than 2.");
            }
            
            _targetSymbol = targetSymbol;
            _referenceSymbol = referenceSymbol;

            _targetDataPoints = new RollingWindow<decimal>(2);
            _referenceDataPoints = new RollingWindow<decimal>(2);

            _targetAlphaReturns = new RollingWindow<double>(alphaPeriod);
            _referenceAlphaReturns = new RollingWindow<double>(alphaPeriod);

            _targetBetaReturns = new RollingWindow<double>(betaPeriod);
            _referenceBetaReturns = new RollingWindow<double>(betaPeriod);
            
            WarmUpPeriod = alphaPeriod + 1;

            _alpha = 0m;
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period})", targetSymbol, referenceSymbol, period, period)
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name"></param>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period})", targetSymbol, referenceSymbol, period, period)
        {
        }
        
        /// <summary>
        /// Computes the next value for this indicator from the given state.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var inputSymbol = input.Symbol;
            if (inputSymbol == _targetSymbol)
            {
                _targetDataPoints.Add(input.Close);
            }
            else if (inputSymbol == _referenceSymbol)
            {
                _referenceDataPoints.Add(input.Close);
            }
            else
            {
                throw new ArgumentException($"The input symbol {inputSymbol} is not the target or reference symbol.");
            }

            if (_targetDataPoints.Samples == _referenceDataPoints.Samples && _targetDataPoints.IsReady && _referenceDataPoints.IsReady)
            {
                _targetAlphaReturns.Add(GetNewReturn(_targetDataPoints));
                _referenceAlphaReturns.Add(GetNewReturn(_referenceDataPoints));

                _targetBetaReturns.Add(GetNewReturn(_targetDataPoints));
                _referenceBetaReturns.Add(GetNewReturn(_referenceDataPoints));

                ComputeAlpha();
            }
            return _alpha;
        }

        /// <summary>
        /// Computes the return of the last two data points of the window
        /// </summary>
        /// <param name="returns"></param>
        /// <returns>Returns the return of the last two data points of the window</returns>
        /// <exception cref="ArgumentException"></exception>
        public static double GetNewReturn(RollingWindow<decimal> returns)
        {
            // Assert that the window has at least two data points
            if (returns.Count < 2)
            {
                throw new ArgumentException("The window must have at least two data points.");
            }

            // Return of the last two data points of the window
            // (last - previous) / previous
            return (double)((returns[0] - returns[1]) / returns[1]);
        }

        /// <summary>
        /// Computes the alpha of the target used in relation with the reference and stores it in the _alpha field
        /// </summary>
        private void ComputeAlpha()
        {
            // Beta = Covariance(TargetReturns, ReferenceReturns) / Variance(ReferenceReturns)

            if (!_targetAlphaReturns.IsReady || !_referenceAlphaReturns.IsReady || !_referenceBetaReturns.IsReady || !_targetBetaReturns.IsReady)
            {
                _alpha = 0;
                return;
            }
            var varianceComputed = _referenceBetaReturns.Variance();
            var covarianceComputed = _targetBetaReturns.Covariance(_referenceBetaReturns);

            // Avoid division with NaN or by zero
            var variance = !varianceComputed.IsNaNOrZero() ? varianceComputed : 1;
            var covariance = !covarianceComputed.IsNaNOrZero() ? covarianceComputed : 0;
            var beta = covariance / variance;

            var targetMean = _targetAlphaReturns.Average();
            var referenceMean = _referenceAlphaReturns.Average();

            // Alpha = TargetMean - Beta * ReferenceMean
            _alpha = !beta.IsNaNOrZero() ? (decimal)(targetMean - beta * referenceMean) : 0;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetDataPoints.Reset();
            _referenceDataPoints.Reset();
            _targetAlphaReturns.Reset();
            _referenceAlphaReturns.Reset();
            _targetBetaReturns.Reset();
            _referenceBetaReturns.Reset();
            _alpha = 0m;
            base.Reset();
        }
    }
}
