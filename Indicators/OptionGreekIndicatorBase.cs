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
using QuantConnect.Logging;
using QuantConnect.Python;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// To provide a base class for option greeks indicator
    /// </summary>
    public abstract class OptionGreeksIndicatorBase : OptionIndicatorBase
    {
        private ImpliedVolatility _iv;
        private bool _userProvidedIv;

        /// <summary>
        /// Cache of the current value of the greek
        /// </summary>
        protected decimal _greekValue { get; set; }

        /// <summary>
        /// Gets the implied volatility of the option
        /// </summary>
        public ImpliedVolatility ImpliedVolatility
        {
            get
            {
                return _iv;
            }
            set
            {
                _iv = value;
                _userProvidedIv = true;
            }
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            Symbol mirrorOption = null, OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel)
        {
            ivModel = GetOptionModel(ivModel, option.ID.OptionStyle);
            WarmUpPeriod = 1;
            _iv = new ImpliedVolatility(name + "_IV", option, riskFreeRateModel, dividendYieldModel, mirrorOption, ivModel.Value);
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            Symbol mirrorOption = null, OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this(name, option, riskFreeRateModel, new ConstantDividendYieldModel(dividendYield), mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this(name, option, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), new ConstantDividendYieldModel(dividendYield),
                  mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel),
                DividendYieldModelPythonWrapper.FromPyObject(dividendYieldModel), mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the OptionGreeksIndicatorBase class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate the Greek</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        protected OptionGreeksIndicatorBase(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel),
                new ConstantDividendYieldModel(dividendYield), mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => ImpliedVolatility.IsReady;

        /// <summary>
        /// Computes the next value of the option greek indicator
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal Calculate(IndicatorDataPoint input)
        {
            var time = input.EndTime;
            var inputSymbol = input.Symbol;

            if (inputSymbol == OptionSymbol)
            {
                if (!_userProvidedIv) ImpliedVolatility.Update(input);
                Price.Update(time, input.Price);
            }
            else if (inputSymbol == _oppositeOptionSymbol)
            {
                if (!_userProvidedIv) ImpliedVolatility.Update(input);
                OppositePrice.Update(time, input.Price);
            }
            else if (inputSymbol == _underlyingSymbol)
            {
                if (!_userProvidedIv) ImpliedVolatility.Update(input);
                UnderlyingPrice.Update(time, input.Price);
            }
            else
            {
                throw new ArgumentException($"The given symbol was not target, reference or underlying symbol: {inputSymbol}");
            }

            if (Price.Current.Time == UnderlyingPrice.Current.Time)
            {
                if (UseMirrorContract)
                {
                    if (Price.Current.Time != OppositePrice.Current.Time)
                    {
                        return _greekValue;
                    }
                }

                RiskFreeRate.Update(time, _riskFreeInterestRateModel.GetInterestRate(time));
                DividendYield.Update(time, _dividendYieldModel.GetDividendYield(time, UnderlyingPrice.Current.Value));

                var timeTillExpiry = Convert.ToDecimal((Expiry - time).TotalDays / 365);
                try
                {
                    _greekValue = timeTillExpiry < 0 ? 0 : CalculateGreek(timeTillExpiry);
                }
                catch (OverflowException)
                {
                    //Log.Error($"OptionGreeksIndicatorBase.Calculate: Decimal overflow detected. The previous greek value will be used.");
                }
            }

            return _greekValue;
        }

        /// <summary>
        /// Calculate the greek of the option
        /// </summary>
        protected abstract decimal CalculateGreek(decimal timeTillExpiry);

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
