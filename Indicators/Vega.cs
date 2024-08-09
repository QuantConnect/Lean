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
using MathNet.Numerics.Distributions;
using Python.Runtime;
using QuantConnect.Data;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Option Vega indicator that calculate the Vega of an option
    /// </summary>
    /// <remarks>sensitivity of option price on IV changes</remarks>
    public class Vega : OptionGreeksIndicatorBase
    {
        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Vega({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Vega({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Vega({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Vega({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRate, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Vega class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Vega(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Vega({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRate, dividendYield,
                  mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Calculate the Vega of the option
        /// </summary>
        protected override decimal CalculateGreek(decimal timeTillExpiry)
        {
            var underlyingPrice = (double)UnderlyingPrice.Current.Value;
            var strike = (double)Strike;
            var timeTillExpiryDouble = (double)timeTillExpiry;
            var riskFreeRate = (double)RiskFreeRate.Current.Value;
            var dividendYield = (double)DividendYield.Current.Value;
            var iv = (double)ImpliedVolatility.Current.Value;

            double result;

            switch (_optionModel)
            {
                case OptionPricingModelType.BinomialCoxRossRubinstein:
                case OptionPricingModelType.ForwardTree:
                    // finite differencing method with 1% IV changes
                    var deltaSigma = 0.01;

                    var newPrice = 0d;
                    var price = 0d;
                    if (_optionModel == OptionPricingModelType.BinomialCoxRossRubinstein)
                    {
                        newPrice = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv + deltaSigma, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                        price = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    }
                    else if (_optionModel == OptionPricingModelType.ForwardTree)
                    {
                        newPrice = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv + deltaSigma, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                        price = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    }

                    result = (newPrice - price) / deltaSigma / 100d;
                    break;

                case OptionPricingModelType.BlackScholes:
                default:
                    var norm = new Normal();
                    var d1 = OptionGreekIndicatorsHelper.CalculateD1(underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, iv);

                    result = underlyingPrice * Math.Sqrt(timeTillExpiryDouble) * norm.Density(d1) * Math.Exp(-dividendYield * timeTillExpiryDouble) / 100d;
                    break;
            }

            return Convert.ToDecimal(result);
        }
    }
}
