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
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
                OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({optionModel})", option, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({optionModel})", option, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
                OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({optionModel})", option, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({optionModel})", option, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, 
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, riskFreeRate, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({optionModel})", option, riskFreeRate, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
                OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, mirrorOption, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{optionModel})", option, mirrorOption, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, mirrorOption, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, PyObject dividendYieldModel,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{optionModel})", option, mirrorOption, riskFreeRateModel, dividendYieldModel, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
                OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, mirrorOption, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, Symbol mirrorOption, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{optionModel})", option, mirrorOption, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, mirrorOption, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, Symbol mirrorOption, PyObject riskFreeRateModel, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{optionModel})", option, mirrorOption, riskFreeRateModel, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(string name, Symbol option, Symbol mirrorOption, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, 
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : base(name, option, mirrorOption, riskFreeRate, dividendYield, optionModel, ivModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Theta class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        public Theta(Symbol option, Symbol mirrorOption, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, OptionPricingModelType? ivModel = null)
            : this($"Theta({option},{mirrorOption},{optionModel})", option, mirrorOption, riskFreeRate, dividendYield, optionModel, ivModel)
        {
        }

        // Calculate the Theta of the option
        protected override decimal CalculateGreek(DateTime time)
        {
            var timeToExpiration = Convert.ToDecimal((Expiry - time).TotalDays) / 365m;
            var math = OptionGreekIndicatorsHelper.DecimalMath;

            switch (_optionModel)
            {
                case OptionPricingModelType.BinomialCoxRossRubinstein:
                case OptionPricingModelType.ForwardTree:
                    var deltaTime = timeToExpiration / OptionGreekIndicatorsHelper.Steps;

                    var forwardPrice = 0m;
                    var price = 0m;
                    if (_optionModel == OptionPricingModelType.BinomialCoxRossRubinstein)
                    {
                        forwardPrice = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeToExpiration - 2 * deltaTime, RiskFreeRate, DividendYield, Right);
                        price = OptionGreekIndicatorsHelper.CRRTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeToExpiration, RiskFreeRate, DividendYield, Right);
                    }
                    else if (_optionModel == OptionPricingModelType.ForwardTree)
                    {
                        forwardPrice = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeToExpiration - 2 * deltaTime, RiskFreeRate, DividendYield, Right); price = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeToExpiration, RiskFreeRate, DividendYield, Right);
                        price = OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(ImpliedVolatility, UnderlyingPrice, Strike, timeToExpiration, RiskFreeRate, DividendYield, Right);
                    }

                    return (forwardPrice - price) * 0.5m / deltaTime / 365m;

                case OptionPricingModelType.BlackScholes:
                default:
                    var norm = new Normal();
                    var d1 = OptionGreekIndicatorsHelper.CalculateD1(UnderlyingPrice, Strike, timeToExpiration, RiskFreeRate, DividendYield, ImpliedVolatility);
                    var d2 = OptionGreekIndicatorsHelper.CalculateD2(d1, ImpliedVolatility, timeToExpiration);
                    var discount = math(Math.Exp, -RiskFreeRate * timeToExpiration);
                    var adjustment = math(Math.Exp, -DividendYield * timeToExpiration);

                    // allow at least 1% IV
                    var theta = -UnderlyingPrice * Math.Max(ImpliedVolatility, 0.01m) * math(norm.Density, d1) * adjustment * 0.5m / math(Math.Sqrt, timeToExpiration);

                    if (Right == OptionRight.Call)
                    {
                        d1 = math(norm.CumulativeDistribution, d1);
                        d2 = -math(norm.CumulativeDistribution, d2);
                    }
                    else
                    {
                        d1 = -math(norm.CumulativeDistribution, -d1);
                        d2 = math(norm.CumulativeDistribution, -d2);
                    }

                    theta += DividendYield * UnderlyingPrice * d1 * adjustment + RiskFreeRate * Strike * discount * d2;
                    return theta / 365m;
            }
        }
    }
}
