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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// To provide a base class for option greeks indicator
    /// </summary>
    public abstract class OptionGreeksIndicatorBase : OptionIndicatorBase
    {
        /// <summary>
        /// Cache of the current value of the greek
        /// </summary>
        protected decimal _greekValue;

        /// <summary>
        /// Gets the implied volatility of the option
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> ImpliedVolatility { get; }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, int period = 2,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, period, optionModel)
        {
            ivModel = ivModel ?? optionModel;
            ImpliedVolatility = new ImpliedVolatility(name + "_IV", option, riskFreeRateModel, period, (OptionPricingModelType)ivModel);
            
            WarmUpPeriod = period;
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, PyObject riskFreeRateModel, int period = 2,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel), period, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, decimal riskFreeRate = 0.05m, int period = 2,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this(name, option, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), period, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => Samples >= 2 && Price.Current.Time == UnderlyingPrice.Current.Time;

        /// <summary>
        /// Computes the next value of the option greek indicator
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            var time = input.EndTime;
            var inputSymbol = input.Symbol;

            if (inputSymbol == _optionSymbol)
            {
                ImpliedVolatility.Update(input);
                Price.Update(time, input.Price);
            }
            else if (inputSymbol == _underlyingSymbol)
            {
                ImpliedVolatility.Update(input);
                UnderlyingPrice.Update(time, input.Price);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            if (Price.Current.Time == UnderlyingPrice.Current.Time)
            {
                RiskFreeRate.Update(time, _riskFreeInterestRateModel.GetInterestRate(time));
                _greekValue = CalculateGreek(time);
            }
            return _greekValue;
        }

        // Calculate the greek of the option
        protected virtual decimal CalculateGreek(DateTime time)
        {
            throw new NotImplementedException("'CalculateGreek' method must be implemented");
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            ImpliedVolatility.Reset();
            base.Reset();
        }
    }
}
