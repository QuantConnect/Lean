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
    /// Option Theta indicator that calculate the theta of an option
    /// </summary>
    /// <remarks>sensitivity of option price on time decay</remarks>
    public class Theta : OptionGreeksIndicatorBase
    {
        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRate, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRate, dividendYield,
                  mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Calculate the Theta of the option
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
                    var deltaTime = timeTillExpiryDouble / OptionGreekIndicatorsHelper.Steps;

                    var forwardPrice = 0d;
                    var price = 0d;
                    if (_optionModel == OptionPricingModelType.BinomialCoxRossRubinstein)
                    {
                        forwardPrice = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble - 2 * deltaTime, riskFreeRate, dividendYield, Right);
                        price = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    }
                    else if (_optionModel == OptionPricingModelType.ForwardTree)
                    {
                        forwardPrice = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble - 2 * deltaTime, riskFreeRate, dividendYield, Right);
                        price = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    }

                    result = (forwardPrice - price) * 0.5 / deltaTime / 365d;
                    break;

                case OptionPricingModelType.BlackScholes:
                default:
                    var norm = new Normal();
                    var d1 = OptionGreekIndicatorsHelper.CalculateD1(underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, iv);
                    var d2 = OptionGreekIndicatorsHelper.CalculateD2(d1, iv, timeTillExpiryDouble);
                    var discount = Math.Exp(-riskFreeRate * timeTillExpiryDouble);
                    var adjustment = Math.Exp(-dividendYield * timeTillExpiryDouble);

                    // allow at least 1% IV
                    var theta = -underlyingPrice * Math.Max(iv, 0.01) * norm.Density(d1) * adjustment * 0.5 / Math.Sqrt(timeTillExpiryDouble);

                    if (Right == OptionRight.Call)
                    {
                        d1 = norm.CumulativeDistribution(d1);
                        d2 = -norm.CumulativeDistribution(d2);
                    }
                    else
                    {
                        d1 = -norm.CumulativeDistribution(-d1);
                        d2 = norm.CumulativeDistribution(-d2);
                    }

                    theta += dividendYield * underlyingPrice * d1 * adjustment + riskFreeRate * strike * discount * d2;
                    result = theta / 365;
                    break;
            }

            return Convert.ToDecimal(result);
        }
    }
}
