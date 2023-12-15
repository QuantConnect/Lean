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

using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Python;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// In financial analysis, the Alpha indicator is used to measure the performance of an investment (such as a stock or ETF) 
    /// relative to a benchmark index, often representing the broader market. Alpha indicates the excess return of the investment 
    /// compared to the return of the benchmark index. 
    /// 
    /// The S P 500 index is frequently used as a benchmark in Alpha calculations to represent the overall market performance. 
    /// Alpha is an essential tool for investors to understand the idiosyncratic returns of their investment that aren't caused
    /// by movement in the underlying benchmark.
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
        /// Period of the indicator - alpha
        /// </summary>
        private readonly decimal _alphaPeriod;

        /// <summary>
        /// Rate of change of the target symbol
        /// </summary>
        private readonly RateOfChange _targetROC;

        /// <summary>
        /// Rate of change of the reference symbol
        /// </summary>
        private readonly RateOfChange _referenceROC;

        /// <summary>
        /// Alpha of the target used in relation with the reference
        /// </summary>
        private decimal _alpha;

        /// <summary>
        /// Beta of the target used in relation with the reference
        /// </summary>
        private readonly Beta _beta;

        /// <summary>
        /// Interest rate model used to compute the risk free rate
        /// </summary>
        private readonly IRiskFreeInterestRateModel _riskFreeInterestRateModel;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; private set; }

        /// <summary>
        /// Gets a flag indicating when the indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetROC.IsReady && _beta.IsReady && _referenceROC.IsReady;

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="alphaPeriod">Period of the indicator - alpha</param>
        /// <param name="betaPeriod">Period of the indicator - beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, IRiskFreeInterestRateModel riskFreeRateModel)
            : base(name)
        {
            // Assert that the target and reference symbols are not the same
            if (targetSymbol == referenceSymbol)
            {
                throw new ArgumentException("The target and reference symbols cannot be the same.");
            }

            // Assert that the period is greater than 2, otherwise the alpha can not be computed
            if (alphaPeriod < 1)
            {
                throw new ArgumentException("The period must be equal or greater than 1.");
            }

            // Assert that the beta period is greater than 2, otherwise the beta can not be computed
            if (betaPeriod < 2)
            {
                throw new ArgumentException("The beta period must be equal or greater than 2.");
            }
            
            _targetSymbol = targetSymbol;
            _referenceSymbol = referenceSymbol;
            _alphaPeriod = alphaPeriod;
            _riskFreeInterestRateModel = riskFreeRateModel;

            _targetROC = new RateOfChange($"{name}_TargetROC", alphaPeriod);
            _referenceROC = new RateOfChange($"{name}_ReferenceROC", alphaPeriod);

            _beta = new Beta($"{name}_Beta", _targetSymbol, _referenceSymbol, betaPeriod);
            
            WarmUpPeriod = alphaPeriod >= betaPeriod ? alphaPeriod + 1 : betaPeriod + 1;

            _alpha = 0m;
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="alphaPeriod">Period of the indicator - alpha</param>
        /// <param name="betaPeriod">Period of the indicator - beta</param>
        /// <param name="riskFreeRate">The risk free rate of this indicator for given period</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, decimal? riskFreeRate = null)
            : this(name, targetSymbol, referenceSymbol, alphaPeriod, betaPeriod, new ConstantRiskFreeRateInterestRateModel(riskFreeRate ?? 0m))
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period values
        /// </summary>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="alphaPeriod">Period of the indicator - alpha</param>
        /// <param name="betaPeriod">Period of the indicator - beta</param>
        /// <param name="riskFreeRate">The risk free rate of this indicator for given period</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, decimal? riskFreeRate = null)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{alphaPeriod},{betaPeriod},{riskFreeRate})", targetSymbol, referenceSymbol, alphaPeriod, betaPeriod, new ConstantRiskFreeRateInterestRateModel(riskFreeRate ?? 0m))
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period value
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        /// <param name="riskFreeRate">The risk free rate of this indicator for given period</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int period, decimal? riskFreeRate = null)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period},{riskFreeRate})", targetSymbol, referenceSymbol, period, period, new ConstantRiskFreeRateInterestRateModel(riskFreeRate ?? 0m))
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        /// <param name="riskFreeRate">The risk free rate of this indicator for given period</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int period, decimal? riskFreeRate = null)
            : this(name, targetSymbol, referenceSymbol, period, period, new ConstantRiskFreeRateInterestRateModel(riskFreeRate ?? 0m))
        {
        }
        
        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period values
        /// </summary>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="alphaPeriod">Period of the indicator - alpha</param>
        /// <param name="betaPeriod">Period of the indicator - beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, IRiskFreeInterestRateModel riskFreeRateModel)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{alphaPeriod},{betaPeriod})", targetSymbol, referenceSymbol, alphaPeriod, betaPeriod, riskFreeRateModel)
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period value
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int period, IRiskFreeInterestRateModel riskFreeRateModel)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period})", targetSymbol, referenceSymbol, period, period, riskFreeRateModel)
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int period, IRiskFreeInterestRateModel riskFreeRateModel)
            : this(name, targetSymbol, referenceSymbol, period, period, riskFreeRateModel)
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period values
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="alphaPeriod">Period of the indicator - alpha</param>
        /// <param name="betaPeriod">Period of the indicator - beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, PyObject riskFreeRateModel)
            : this(name, targetSymbol, referenceSymbol, alphaPeriod, betaPeriod, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel))
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period values
        /// </summary>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="alphaPeriod">Period of the indicator - alpha</param>
        /// <param name="betaPeriod">Period of the indicator - beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int alphaPeriod, int betaPeriod, PyObject riskFreeRateModel)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{alphaPeriod},{betaPeriod})", targetSymbol, referenceSymbol, alphaPeriod, betaPeriod, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel))
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified target, reference, and period value
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(Symbol targetSymbol, Symbol referenceSymbol, int period, PyObject riskFreeRateModel)
            : this($"ALPHA({targetSymbol},{referenceSymbol},{period})", targetSymbol, referenceSymbol, period, period, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel))
        {
        }

        /// <summary>
        /// Creates a new Alpha indicator with the specified name, target, reference, and period value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="targetSymbol"></param>
        /// <param name="referenceSymbol"></param>
        /// <param name="period">Period of the indicator - alpha and beta</param>
        /// <param name="riskFreeRateModel">The risk free rate model of this indicator</param>
        public Alpha(string name, Symbol targetSymbol, Symbol referenceSymbol, int period, PyObject riskFreeRateModel)
            : this(name, targetSymbol, referenceSymbol, period, period, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel))
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
            var inputSymbol = input.Symbol;

            if (inputSymbol == _targetSymbol)
            {
                _targetROC.Update(input.EndTime, input.Close);
            }
            else if (inputSymbol == _referenceSymbol)
            {
                _referenceROC.Update(input.EndTime, input.Close);
            }
            else
            {
                throw new ArgumentException($"The input symbol {inputSymbol} is not the target or reference symbol.");
            }

            _beta.Update(input);
            
            if (_targetROC.Samples == _referenceROC.Samples && _referenceROC.Samples > 0)
            {
                ComputeAlpha();
            }

            return _alpha;
        }

        /// <summary>
        /// Computes the alpha of the target used in relation with the reference and stores it in the _alpha field
        /// </summary>
        private void ComputeAlpha()
        {
            if (!_beta.IsReady || !_targetROC.IsReady || !_referenceROC.IsReady)
            {
                _alpha = 0m;
                return;
            }

            var targetMean = _targetROC.Current.Value / _alphaPeriod;
            var referenceMean = _referenceROC.Current.Value / _alphaPeriod;

            var riskFreeRate = _riskFreeInterestRateModel.GetInterestRate(_targetROC.Current.Time);

            _alpha = targetMean - (riskFreeRate + _beta.Current.Value * (referenceMean - riskFreeRate));
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetROC.Reset();
            _referenceROC.Reset();
            _beta.Reset();
            _alpha = 0m;
            base.Reset();
        }
    }
}
