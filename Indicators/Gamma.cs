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
    /// Option Gamma indicator that calculate the gamma of an option
    /// </summary>
    /// <remarks>derivative of option price change relative to $1 underlying changes</remarks>
    public class Gamma : OptionGreeksIndicatorBase
    {
        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRate, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Gamma class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Gamma(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRate, dividendYield,
                  mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Calculate the Gamma of the option
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
                case OptionPricingModelType.BlackScholes:
                    var norm = new Normal();
                    var d1 = OptionGreekIndicatorsHelper.CalculateD1(underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, iv);

                    // allow at least 1% IV
                    result = norm.Density(-d1) / underlyingPrice / Math.Max(iv, 0.01) / Math.Sqrt(timeTillExpiryDouble);
                    break;

                case OptionPricingModelType.BinomialCoxRossRubinstein:
                case OptionPricingModelType.ForwardTree:
                    var upFactor = Math.Exp(iv * Math.Sqrt(timeTillExpiryDouble / OptionGreekIndicatorsHelper.Steps));
                    if (upFactor == 1)
                    {
                        // provide a small step to estimate gamma
                        upFactor = 1.0001;
                    }

                    // Finite differencing approach
                    var sU = underlyingPrice * upFactor * upFactor;
                    var sD = underlyingPrice / upFactor / upFactor;

                    var fU = 0d;
                    var fM = 0d;
                    var fD = 0d;
                    if (_optionModel == OptionPricingModelType.BinomialCoxRossRubinstein)
                    {
                        fU = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, sU, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                        fM = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                        fD = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, sD, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    }
                    else if (_optionModel == OptionPricingModelType.ForwardTree)
                    {
                        fU = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, sU, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                        fM = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                        fD = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, sD, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    }

                    var gammaU = (fU - fM) / (sU - underlyingPrice);
                    var gammaD = (fM - fD) / (underlyingPrice - sD);

                    result = OptionGreekIndicatorsHelper.Divide((gammaU - gammaD) * 2, sU - sD);
                    break;

                default:
                    throw new Exception("Unrecognized Option Pricing Model");
            }

            return Convert.ToDecimal(result);
        }
    }
}
