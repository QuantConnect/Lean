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
using MathNet.Numerics.RootFinding;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Python;
using QuantConnect.Util;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Implied Volatility indicator that calculate the IV of an option using Black-Scholes Model
    /// </summary>
    public class ImpliedVolatility : OptionIndicatorBase
    {
        private decimal _impliedVolatility;
        private Func<decimal, decimal, decimal> SmoothingFunction;

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : base(name, option, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel)
        {
            if (mirrorOption != null)
            {
                // Default smoothing function will be assuming Law of One Price hold,
                // so both call and put will have the same IV
                // and using on OTM/ATM options to calculate the IV
                // by assuming extra volatility coming from extrinsic value
                SmoothingFunction = (impliedVol, mirrorImpliedVol) =>
                {
                    if (Strike > UnderlyingPrice && Right == OptionRight.Put)
                    {
                        return mirrorImpliedVol;
                    }
                    else if (Strike < UnderlyingPrice && Right == OptionRight.Call)
                    {
                        return mirrorImpliedVol;
                    }
                    return impliedVol;
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel,
            Symbol mirrorOption = null, OptionPricingModelType? optionModel = null)
            : this($"IV({option},{mirrorOption},{riskFreeRateModel},{dividendYieldModel},{optionModel})", option, riskFreeRateModel,
                  dividendYieldModel, mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel),
                DividendYieldModelPythonWrapper.FromPyObject(dividendYieldModel), mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYieldModel">Dividend yield model</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, PyObject riskFreeRateModel, PyObject dividendYieldModel, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this($"IV({option},{mirrorOption},{riskFreeRateModel},{dividendYieldModel},{optionModel})", option,
                  riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this(name, option, riskFreeRateModel, new ConstantDividendYieldModel(dividendYield), mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, IRiskFreeInterestRateModel riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this($"IV({option},{mirrorOption},{riskFreeRateModel},{dividendYield},{optionModel})", option, riskFreeRateModel, dividendYield,
                  mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this(name, option, RiskFreeInterestRateModelPythonWrapper.FromPyObject(riskFreeRateModel),
                new ConstantDividendYieldModel(dividendYield), mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRateModel">Risk-free rate model</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, PyObject riskFreeRateModel, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this($"IV({option},{mirrorOption},{riskFreeRateModel},{dividendYield},{optionModel})", option, riskFreeRateModel,
                  dividendYield, mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this(name, option, new ConstantRiskFreeRateInterestRateModel(riskFreeRate), new ConstantDividendYieldModel(dividendYield), mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">Risk-free rate, as a constant</param>
        /// <param name="dividendYield">Dividend yield, as a constant</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
            OptionPricingModelType? optionModel = null)
            : this($"IV({option},{mirrorOption},{riskFreeRate},{dividendYield},{optionModel})", option, riskFreeRate,
                  dividendYield, mirrorOption, optionModel)
        {
        }

        /// <summary>
        /// Set the smoothing function of IV, using both call and put IV value
        /// </summary>
        /// <param name="function">the smoothing function</param>
        public void SetSmoothingFunction(Func<decimal, decimal, decimal> function)
        {
            SmoothingFunction = function;
        }

        /// <summary>
        /// Set the smoothing function of IV, using both call and put IV value
        /// </summary>
        /// <param name="function">the smoothing function</param>
        public void SetSmoothingFunction(PyObject function)
        {
            SmoothingFunction = PythonUtil.ToFunc<decimal, decimal, decimal>(function);
        }

        /// <summary>
        /// Computes the next value
        /// </summary>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeIndicator()
        {
            var time = Price.Current.EndTime;

            RiskFreeRate.Update(time, _riskFreeInterestRateModel.GetInterestRate(time));
            DividendYield.Update(time, _dividendYieldModel.GetDividendYield(time, UnderlyingPrice.Current.Value));

            var timeTillExpiry = Convert.ToDecimal(OptionGreekIndicatorsHelper.TimeTillExpiry(Expiry, time));
            _impliedVolatility = CalculateIV(timeTillExpiry);

            return _impliedVolatility;
        }

        // Calculate the theoretical option price
        private static double TheoreticalPrice(double volatility, double spotPrice, double strikePrice, double timeTillExpiry, double riskFreeRate,
            double dividendYield, OptionRight optionType, OptionPricingModelType? optionModel = null)
        {
            if (timeTillExpiry <= 0)
            {
                return 0;
            }

            return optionModel switch
            {
                // Binomial model also follows BSM process (log-normal)
                OptionPricingModelType.BinomialCoxRossRubinstein => OptionGreekIndicatorsHelper.CRRTheoreticalPrice(volatility, spotPrice, strikePrice, timeTillExpiry, riskFreeRate, dividendYield, optionType),
                OptionPricingModelType.ForwardTree => OptionGreekIndicatorsHelper.ForwardTreeTheoreticalPrice(volatility, spotPrice, strikePrice, timeTillExpiry, riskFreeRate, dividendYield, optionType),
                _ => OptionGreekIndicatorsHelper.BlackTheoreticalPrice(volatility, spotPrice, strikePrice, timeTillExpiry, riskFreeRate, dividendYield, optionType),
            };
        }

        /// <summary>
        /// Computes the IV of the option
        /// </summary>
        /// <param name="timeTillExpiry">the time until expiration in years</param>
        /// <returns>Smoothened IV of the option</returns>
        protected virtual decimal CalculateIV(decimal timeTillExpiry)
        {
            var underlyingPrice = (double)UnderlyingPrice.Current.Value;
            var strike = (double)Strike;
            var timeTillExpiryDouble = (double)timeTillExpiry;
            var riskFreeRate = (double)RiskFreeRate.Current.Value;
            var dividendYield = (double)DividendYield.Current.Value;
            var optionPrice = (double)Price.Current.Value;

            var impliedVol = CalculateIV(OptionSymbol, strike, timeTillExpiryDouble, Right, optionPrice, underlyingPrice, riskFreeRate,
                dividendYield, _optionModel);

            if (UseMirrorContract)
            {
                var mirrorOptionPrice = (double)OppositePrice.Current.Value;

                var mirrorImpliedVol = CalculateIV(_oppositeOptionSymbol, strike, timeTillExpiryDouble, _oppositeOptionSymbol.ID.OptionRight,
                        mirrorOptionPrice, underlyingPrice, riskFreeRate, dividendYield, _optionModel);

                if (mirrorImpliedVol.HasValue)
                {
                    if (impliedVol.HasValue)
                    {
                        // use 'SmoothingFunction' if both calculations succeeded
                        return SmoothingFunction(impliedVol.Value, mirrorImpliedVol.Value);
                    }
                    return mirrorImpliedVol.Value;
                }
            }

            return impliedVol ?? 0;
        }

        private decimal? CalculateIV(Symbol optionSymbol, double strike, double timeTillExpiry, OptionRight right, double optionPrice, double underlyingPrice,
            double riskFreeRate, double dividendYield, OptionPricingModelType optionModel)
        {
            GetRootFindingMethodParameters(optionSymbol, strike, timeTillExpiry, optionPrice, underlyingPrice, riskFreeRate, dividendYield,
                optionModel, out var accuracy, out var lowerBound, out var upperBound);

            decimal? impliedVol = null;
            try
            {
                Func<double, double> f = (vol) => TheoreticalPrice(vol, underlyingPrice, strike, timeTillExpiry, riskFreeRate, dividendYield, right, optionModel) - optionPrice;
                impliedVol = Convert.ToDecimal(Brent.FindRoot(f, lowerBound, upperBound, accuracy, 100));
            }
            catch
            {
                Log.Error("ImpliedVolatility.CalculateIV(): Fail to converge, returning 0.");
            }

            return impliedVol;
        }

        private void GetRootFindingMethodParameters(Symbol optionSymbol, double strike, double timeTillExpiry, double optionPrice,
            double underlyingPrice, double riskFreeRate, double dividendYield, OptionPricingModelType optionModel,
            out double accuracy, out double lowerBound, out double upperBound)
        {
            // Set the accuracy as a factor of the option price when possible
            accuracy = Math.Max(1e-4, 1e-4 * optionPrice);
            lowerBound = 1e-7;
            upperBound = 4.0;

            // Use BSM as initial guess to get a better range for root finding
            if (optionModel != OptionPricingModelType.BlackScholes)
            {
                var initialGuess = (double)(CalculateIV(optionSymbol, strike, timeTillExpiry, optionSymbol.ID.OptionRight, optionPrice,
                    underlyingPrice, riskFreeRate, dividendYield, OptionPricingModelType.BlackScholes) ?? 0);
                if (initialGuess != 0)
                {
                    lowerBound = Math.Max(lowerBound, initialGuess * 0.5);
                    upperBound = Math.Min(upperBound, initialGuess * 1.5);
                }
            }
        }
    }
}
