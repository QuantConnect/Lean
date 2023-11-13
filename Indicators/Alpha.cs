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
using System.Linq;

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
        /// Symbol of the reference used
        /// </summary>
        private readonly Symbol _referenceSymbol;

        /// <summary>
        /// Symbol of the target used
        /// </summary>
        private readonly Symbol _targetSymbol;

        /// <summary>
        /// Rate of change of the target symbol
        /// </summary>
        private readonly RateOfChange _targetROC;

        /// <summary>
        /// Rate of change of the reference symbol
        /// </summary>
        private readonly RateOfChange _referenceROC;

        /// <summary>
        /// RollingWindow of returns of the target symbol in the given period for the alpha
        /// </summary>
        private readonly RollingWindow<decimal> _targetReturns;

        /// <summary>
        /// RollingWindow of returns of the reference symbol in the given period for the alpha
        /// </summary>
        private readonly RollingWindow<decimal> _referenceReturns;

        /// <summary>
        /// Alpha of the target used in relation with the reference
        /// </summary>
        private decimal _alpha;

        /// <summary>
        /// Beta of the target used in relation with the reference
        /// </summary>
        private readonly Beta _beta;

        private readonly decimal _riskFreeRate;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetReturns.IsReady && _beta.IsReady && _referenceReturns.IsReady;

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="alphaPeriod">The Alpha period of this indicator</param>
        /// <param name="betaPeriod">The Beta period of this indicator</param>
        /// <param name="riskFreeRate">The risk free rate of this indicator for given period</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, decimal riskFreeRate = 0.0m)
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

            _targetROC = new RateOfChange($"{name}_TargetROC", 1);
            _referenceROC = new RateOfChange($"{name}_ReferenceROC", 1);

            _targetReturns = new RollingWindow<decimal>(alphaPeriod);
            _referenceReturns = new RollingWindow<decimal>(alphaPeriod);

            _beta = new Beta($"{name}_Beta", _targetSymbol, _referenceSymbol, betaPeriod);
            
            WarmUpPeriod = alphaPeriod >= betaPeriod ? alphaPeriod + 1 : betaPeriod + 1;

            _alpha = 0m;
            _riskFreeRate = riskFreeRate;
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period values
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int period, decimal riskFreeRate = 0.0m)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period},{riskFreeRate})", targetSymbol, referenceSymbol, period, period, riskFreeRate)
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name"></param>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int period, decimal riskFreeRate = 0.0m)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period},{riskFreeRate})", targetSymbol, referenceSymbol, period, period, riskFreeRate)
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
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            Symbol inputSymbol = input.Symbol;
            
            if (inputSymbol == _targetSymbol)
            {
                _targetROC.Update(input.EndTime, input.Close);
                if (_targetROC.IsReady)
                    _targetReturns.Add(_targetROC.Current.Value);
            }
            else if (inputSymbol == _referenceSymbol)
            {
                _referenceROC.Update(input.EndTime, input.Close);
                if (_referenceROC.IsReady)
                    _referenceReturns.Add(_referenceROC.Current.Value);
            }
            else
            {
                throw new ArgumentException($"The input symbol {inputSymbol} is not the target or reference symbol.");
            }

            _beta.Update(input);
            
            if (_targetReturns.Samples == _referenceReturns.Samples && _targetReturns.Samples > 0)
                ComputeAlpha();

            return _alpha;
        }

        /// <summary>
        /// Computes the alpha of the target used in relation with the reference and stores it in the _alpha field
        /// </summary>
        private void ComputeAlpha()
        {
            if (!_beta.IsReady || !_targetReturns.IsReady || !_referenceReturns.IsReady)
            {
                _alpha = 0m;
                return;
            }

            var targetMean = _targetReturns.Average();
            var referenceMean = _referenceReturns.Average();

            _alpha = targetMean - (_riskFreeRate + _beta.Current.Value * (referenceMean - _riskFreeRate));
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetReturns.Reset();
            _referenceReturns.Reset();
            _targetROC.Reset();
            _referenceROC.Reset();
            _beta.Reset();
            _alpha = 0m;
            base.Reset();
        }
    }
}
