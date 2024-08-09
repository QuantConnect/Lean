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
    /// Option Delta indicator that calculate the delta of an option
    /// </summary>
    /// <remarks>sensitivity of option price relative to $1 of underlying change</remarks>
    public class Delta : OptionGreeksIndicatorBase
    {
        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Delta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Delta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
                OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Delta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Delta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRate, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Delta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Delta(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null, OptionPricingModelType? ivModel = null)
            : this($"Delta({option},{mirrorOption},{GetOptionModel(optionModel, option.ID.OptionStyle)})", option, riskFreeRate, dividendYield,
                  mirrorOption, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Calculate the Delta of the option
        /// </summary>
        protected override decimal CalculateGreek(decimal timeTillExpiry)
        {
            var iv = (double)ImpliedVolatility.Current.Value;
            var underlyingPrice = (double)UnderlyingPrice.Current.Value;
            var strike = (double)Strike;
            var timeTillExpiryDouble = (double)timeTillExpiry;
            var riskFreeRate = (double)RiskFreeRate.Current.Value;
            var dividendYield = (double)DividendYield.Current.Value;

            double result;

            switch (_optionModel)
            {
                case OptionPricingModelType.BinomialCoxRossRubinstein:
                    var upFactor = Math.Exp(iv * Math.Sqrt(timeTillExpiryDouble / OptionGreekIndicatorsHelper.Steps));
                    if (upFactor == 1)
                    {
                        // provide a small step to estimate delta
                        upFactor = 1.00001;
                    }

                    var sU = underlyingPrice * upFactor;
                    var sD = underlyingPrice / upFactor;

                    var fU = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, sU, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    var fD = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(iv, sD, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);

                    result = OptionGreekIndicatorsHelper.Divide(fU - fD, sU - sD);
                    break;

                case OptionPricingModelType.ForwardTree:
                    var discount = Math.Exp((riskFreeRate - dividendYield) * timeTillExpiryDouble / OptionGreekIndicatorsHelper.Steps);
                    upFactor = Math.Exp(iv * Math.Sqrt(timeTillExpiryDouble / OptionGreekIndicatorsHelper.Steps)) * discount;
                    if (upFactor == 1)
                    {
                        // provide a small step to estimate delta
                        upFactor = 1.00001;
                    }
                    var downFactor = Math.Exp(-iv * Math.Sqrt(timeTillExpiryDouble / OptionGreekIndicatorsHelper.Steps)) * discount;
                    if (downFactor == 1)
                    {
                        // provide a small step to estimate delta
                        downFactor = 0.99999;
                    }

                    sU = underlyingPrice * upFactor;
                    sD = underlyingPrice * downFactor;

                    fU = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, sU, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);
                    fD = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(iv, sD, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, Right);

                    result = OptionGreekIndicatorsHelper.Divide(fU - fD, sU - sD);
                    break;

                case OptionPricingModelType.BlackScholes:
                default:
                    var norm = new Normal();
                    var d1 = OptionGreekIndicatorsHelper.CalculateD1(underlyingPrice, strike, timeTillExpiryDouble, riskFreeRate, dividendYield, iv);

                    double wholeShareDelta;
                    if (Right == OptionRight.Call)
                    {
                        wholeShareDelta = norm.CumulativeDistribution(d1);
                    }
                    else
                    {
                        wholeShareDelta = -norm.CumulativeDistribution(-d1);
                    }

                    result = wholeShareDelta * Math.Exp(-dividendYield * timeTillExpiryDouble);
                    break;
            }

            return Convert.ToDecimal(result);
        }
    }
}
