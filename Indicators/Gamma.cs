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
                OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{optionModel})", option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{optionModel})", option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel)
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
                OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{optionModel})", option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{optionModel})", option, riskFreeRateModel, dividendYield, mirrorOption, optionModel, ivModel)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
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
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Gamma({option},{mirrorOption},{optionModel})", option, riskFreeRate, dividendYield, mirrorOption, optionModel, ivModel)
        {
        }

        // Calculate the Gamma of the option
        protected override decimal CalculateGreek(decimal timeTillExpiry)
        {
            var math = OptionGreekIndicatorsHelper.DecimalMath;

            switch (_optionModel)
            {
                case OptionPricingModelType.BlackScholes:
                    var norm = new Normal();
                    var d1 = OptionGreekIndicatorsHelper.CalculateD1(UnderlyingPrice, Strike, timeTillExpiry, RiskFreeRate, DividendYield, ImpliedVolatility);

                    // allow at least 1% IV
                    return math(norm.Density, -d1) / UnderlyingPrice / Math.Max(ImpliedVolatility, 0.01m) / math(Math.Sqrt, timeTillExpiry);

                case OptionPricingModelType.BinomialCoxRossRubinstein:
                case OptionPricingModelType.ForwardTree:
                    var upFactor = math(Math.Exp, ImpliedVolatility * math(Math.Sqrt, timeTillExpiry / OptionGreekIndicatorsHelper.Steps));
                    if (upFactor == 1)
                    {
                        // provide a small step to estimate gamma
                        upFactor = 1.0001m;
                    }

                    // Finite differncing approach
                    var sU = UnderlyingPrice * upFactor * upFactor;
                    var sD = UnderlyingPrice / upFactor / upFactor;

                    var fU = 0m;
                    var fM = 0m;
                    var fD = 0m;
                    if (_optionModel == OptionPricingModelType.BinomialCoxRossRubinstein)
                    {
                        fU = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(ImpliedVolatility, sU, Strike, timeTillExpiry, RiskFreeRate, DividendYield, Right);
                        fM = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeTillExpiry, RiskFreeRate, DividendYield, Right);
                        fD = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(ImpliedVolatility, sD, Strike, timeTillExpiry, RiskFreeRate, DividendYield, Right);
                    }
                    else if (_optionModel == OptionPricingModelType.ForwardTree)
                    {
                        fU = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(ImpliedVolatility, sU, Strike, timeTillExpiry, RiskFreeRate, DividendYield, Right);
                        fM = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeTillExpiry, RiskFreeRate, DividendYield, Right);
                        fD = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(ImpliedVolatility, sD, Strike, timeTillExpiry, RiskFreeRate, DividendYield, Right);
                    }

                    var gammaU = (fU - fM) / (sU - UnderlyingPrice);
                    var gammaD = (fM - fD) / (UnderlyingPrice - sD);

                    return (gammaU - gammaD) * 2 / (sU - sD);

                default:
                    throw new Exception("Unrecognized Option Pricing Model");
            }
        }
    }
}
