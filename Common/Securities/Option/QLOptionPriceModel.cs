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
 *
*/

using QLNet;
using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Securities.Option
{
    using PricingEngineFunc = Func<GeneralizedBlackScholesProcess, IPricingEngine>;
    using PricingEngineFuncEx = Func<Symbol, GeneralizedBlackScholesProcess, IPricingEngine>;

    /// <summary>
    /// Provides QuantLib(QL) implementation of <see cref="IOptionPriceModel"/> to support major option pricing models, available in QL.
    /// </summary>
    public class QLOptionPriceModel : IOptionPriceModel
    {
        private static readonly OptionStyle[] _defaultAllowedOptionStyles = { OptionStyle.European, OptionStyle.American };
        private static readonly IQLUnderlyingVolatilityEstimator _defaultUnderlyingVolEstimator = new ConstantQLUnderlyingVolatilityEstimator();
        private static readonly IQLRiskFreeRateEstimator _defaultRiskFreeRateEstimator = new FedRateQLRiskFreeRateEstimator();
        private static readonly IQLDividendYieldEstimator _defaultDividendYieldEstimator = new ConstantQLDividendYieldEstimator();

        private readonly IQLUnderlyingVolatilityEstimator _underlyingVolEstimator;
        private readonly IQLDividendYieldEstimator _dividendYieldEstimator;
        private readonly IQLRiskFreeRateEstimator _riskFreeRateEstimator;
        private readonly PricingEngineFuncEx _pricingEngineFunc;

        /// <summary>
        /// When enabled, approximates Greeks if corresponding pricing model didn't calculate exact numbers.
        /// The default value is true.
        /// </summary>
        public bool EnableGreekApproximation { get; set; } = true;

        /// <summary>
        /// True if volatility model is warmed up, i.e. has generated volatility value different from zero, otherwise false.
        /// </summary>
        public bool VolatilityEstimatorWarmedUp => _underlyingVolEstimator.IsReady;

        /// <summary>
        /// List of option styles supported by the pricing model.
        /// By default, both American and European option styles are supported.
        /// </summary>
        public IReadOnlyCollection<OptionStyle> AllowedOptionStyles { get; }

        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        /// <param name="allowedOptionStyles">List of option styles supported by the pricing model. It defaults to both American and European option styles</param>
        public QLOptionPriceModel(PricingEngineFunc pricingEngineFunc, 
                                  IQLUnderlyingVolatilityEstimator underlyingVolEstimator = null, 
                                  IQLRiskFreeRateEstimator riskFreeRateEstimator = null, 
                                  IQLDividendYieldEstimator dividendYieldEstimator = null, 
                                  OptionStyle[] allowedOptionStyles = null)
            : this((option, process) => pricingEngineFunc(process), underlyingVolEstimator, riskFreeRateEstimator, dividendYieldEstimator, allowedOptionStyles)
        {}
        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function takes option and modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        /// <param name="allowedOptionStyles">List of option styles supported by the pricing model. It defaults to both American and European option styles</param>
        public QLOptionPriceModel(PricingEngineFuncEx pricingEngineFunc, 
                                  IQLUnderlyingVolatilityEstimator underlyingVolEstimator = null, 
                                  IQLRiskFreeRateEstimator riskFreeRateEstimator = null, 
                                  IQLDividendYieldEstimator dividendYieldEstimator = null, 
                                  OptionStyle[] allowedOptionStyles = null)
        {
            _pricingEngineFunc = pricingEngineFunc;
            _underlyingVolEstimator = underlyingVolEstimator ?? _defaultUnderlyingVolEstimator;
            _riskFreeRateEstimator = riskFreeRateEstimator ?? _defaultRiskFreeRateEstimator;
            _dividendYieldEstimator = dividendYieldEstimator ?? _defaultDividendYieldEstimator;

            AllowedOptionStyles = allowedOptionStyles ?? _defaultAllowedOptionStyles;
        }

        /// <summary>
        /// Evaluates the specified option contract to compute a theoretical price, IV and greeks
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">The current data slice. This can be used to access other information
        /// available to the algorithm</param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>An instance of <see cref="OptionPriceModelResult"/> containing the theoretical
        /// price of the specified option contract</returns>
        public OptionPriceModelResult Evaluate(Security security, Slice slice, OptionContract contract)
        {
            if (!AllowedOptionStyles.Contains(contract.Symbol.ID.OptionStyle))
            {
               throw new ArgumentException($"{contract.Symbol.ID.OptionStyle} style options are not supported by option price model '{this.GetType().Name}'");
            }

            try
            {
                // expired options have no price
                if (contract.Time.Date > contract.Expiry.Date)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"QLOptionPriceModel.Evaluate(). Expired {contract.Symbol}. Time > Expiry: {contract.Time.Date} > {contract.Expiry.Date}");
                    }
                    return OptionPriceModelResult.None;
                }

                var dayCounter = new Actual365Fixed();
                var securityExchangeHours = security.Exchange.Hours;
                var maturityDate = AddDays(contract.Expiry.Date, Option.DefaultSettlementDays, securityExchangeHours);

                // Get time until maturity (in year)
                var maturity = dayCounter.yearFraction(contract.Time.Date, maturityDate);
                if (maturity < 0)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"QLOptionPriceModel.Evaluate(). negative time ({maturity}) given for {contract.Symbol}. Time: {contract.Time.Date}. Maturity {maturityDate}");
                    }
                    return OptionPriceModelResult.None;
                }

                // setting up option pricing parameters
                var optionSecurity = (Option)security;
                var premium = (double)optionSecurity.Price;
                var spot = (double)optionSecurity.Underlying.Price;

                if (spot <= 0d || premium <= 0d)
                {
                    if (Log.DebuggingEnabled)
                    {
                        Log.Debug($"QLOptionPriceModel.Evaluate(). Non-positive prices for {contract.Symbol}. Premium: {premium}. Underlying price {spot}");
                    }

                    return OptionPriceModelResult.None;
                }

                var calendar = new UnitedStates();
                var settlementDate = AddDays(contract.Time.Date, Option.DefaultSettlementDays, securityExchangeHours);
                var underlyingQuoteValue = new SimpleQuote(spot);

                var dividendYieldValue = new SimpleQuote(_dividendYieldEstimator.Estimate(security, slice, contract));
                var dividendYield = new Handle<YieldTermStructure>(new FlatForward(0, calendar, dividendYieldValue, dayCounter));

                var riskFreeRateValue = new SimpleQuote((double)_riskFreeRateEstimator.Estimate(security, slice, contract));
                var riskFreeRate = new Handle<YieldTermStructure>(new FlatForward(0, calendar, riskFreeRateValue, dayCounter));

                // Get discount factor by dividend and risk free rate using the maturity
                var dividendDiscount = dividendYield.link.discount(maturity);
                var riskFreeDiscount = riskFreeRate.link.discount(maturity);
                var forwardPrice = spot * dividendDiscount / riskFreeDiscount;

                // Initial guess for volatility by Brenner and Subrahmanyam (1988)
                var initialGuess = Math.Sqrt(2 * Math.PI / maturity) * premium / spot;

                var underlyingVolEstimate = _underlyingVolEstimator.Estimate(security, slice, contract);
                
                // If the volatility estimator is not ready, we will use initial guess
                if (!_underlyingVolEstimator.IsReady)
                {
                    underlyingVolEstimate = initialGuess;
                }

                var underlyingVolValue = new SimpleQuote(underlyingVolEstimate);
                var underlyingVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(0, calendar, new Handle<Quote>(underlyingVolValue), dayCounter));

                // preparing stochastic process and payoff functions
                var stochasticProcess = new BlackScholesMertonProcess(new Handle<Quote>(underlyingQuoteValue), dividendYield, riskFreeRate, underlyingVol);
                var payoff = new PlainVanillaPayoff(contract.Right == OptionRight.Call ? QLNet.Option.Type.Call : QLNet.Option.Type.Put, (double)contract.Strike);

                // creating option QL object
                var option = contract.Symbol.ID.OptionStyle == OptionStyle.American ?
                            new VanillaOption(payoff, new AmericanExercise(settlementDate, maturityDate)) :
                            new VanillaOption(payoff, new EuropeanExercise(maturityDate));

                // preparing pricing engine QL object
                option.setPricingEngine(_pricingEngineFunc(contract.Symbol, stochasticProcess));

                // Setting the evaluation date before running the calculations
                var evaluationDate = contract.Time.Date;
                SetEvaluationDate(evaluationDate);
                
                // running calculations
                var npv = EvaluateOption(option);

                BlackCalculator blackCalculator = null;

                // Calculate the Implied Volatility
                var impliedVol = 0d;
                try
                {
                    SetEvaluationDate(evaluationDate);
                    impliedVol = option.impliedVolatility(premium, stochasticProcess);
                }
                catch (Exception e)
                {
                    // A Newton-Raphson optimization estimate of the implied volatility
                    impliedVol = ImpliedVolatilityEstimation(premium, initialGuess, maturity, riskFreeDiscount, forwardPrice, payoff, out blackCalculator);
                    if (Log.DebuggingEnabled)
                    {
                        var referenceDate = underlyingVol.link.referenceDate();
                        Log.Debug($"QLOptionPriceModel.Evaluate(). Cannot calculate Implied Volatility for {contract.Symbol}. Implied volatility from Newton-Raphson optimization: {impliedVol}. Premium: {premium}. Underlying price: {spot}. Initial guess volatility: {initialGuess}. Maturity: {maturity}. Risk Free: {riskFreeDiscount}. Forward price: {forwardPrice}. Data time: {evaluationDate}. Reference date: {referenceDate}. {e.Message} {e.StackTrace}");
                    }
                }

                // Update the Black Vol Term Structure with the Implied Volatility to improve Greek calculation
                // We assume that the underlying volatility model does not yield a good estimate and 
                // other sources, e.g. Interactive Brokers, use the implied volatility to calculate the Greeks
                // After this operation, the Theoretical Price (NPV) will match the Premium, so we do not re-evalute
                // it and let users compare NPV and the Premium if they wish. 
                underlyingVolValue.setValue(impliedVol);

                // function extracts QL greeks catching exception if greek is not generated by the pricing engine and reevaluates option to get numerical estimate of the seisitivity
                decimal tryGetGreekOrReevaluate(Func<double> greek, Func<BlackCalculator, double> black)
                {
                    double result;
                    var isApproximation = false;
                    Exception exception = null;

                    try
                    {
                        SetEvaluationDate(evaluationDate);
                        result = greek();
                    }
                    catch (Exception err)
                    {
                        exception = err;

                        if (!EnableGreekApproximation)
                        {
                            return 0.0m;
                        }

                        if (blackCalculator == null)
                        {
                            // Define Black Calculator to calculate Greeks that are not defined by the option object
                            // Some models do not evaluate all greeks under some circumstances (e.g. low dividend yield)
                            // We override this restriction to calculate the Greeks directly with the BlackCalculator
                            var vol = underlyingVol.link.blackVol(maturityDate, (double)contract.Strike);
                            blackCalculator = CreateBlackCalculator(forwardPrice, riskFreeDiscount, vol, payoff);
                        }

                        isApproximation = true;
                        result = black(blackCalculator);
                    }

                    if (result.IsNaNOrInfinity())
                    {
                        if (Log.DebuggingEnabled)
                        {
                            var referenceDate = underlyingVol.link.referenceDate();
                            Log.Debug($"QLOptionPriceModel.Evaluate(). NaN or Infinity greek for {contract.Symbol}. Premium: {premium}. Underlying price: {spot}. Initial guess volatility: {initialGuess}. Maturity: {maturity}. Risk Free: {riskFreeDiscount}. Forward price: {forwardPrice}. Implied Volatility: {impliedVol}. Is Approximation? {isApproximation}. Data time: {evaluationDate}. Reference date: {referenceDate}. {exception?.Message} {exception?.StackTrace}");
                        }

                        return 0m;
                    }

                    var value = result.SafeDecimalCast();

                    if (value == decimal.Zero && Log.DebuggingEnabled)
                    {
                        var referenceDate = underlyingVol.link.referenceDate();
                        Log.Debug($"QLOptionPriceModel.Evaluate(). Zero-value greek for {contract.Symbol}. Premium: {premium}. Underlying price: {spot}. Initial guess volatility: {initialGuess}. Maturity: {maturity}. Risk Free: {riskFreeDiscount}. Forward price: {forwardPrice}. Implied Volatility: {impliedVol}. Is Approximation? {isApproximation}. Data time: {evaluationDate}. Reference date: {referenceDate}. {exception?.Message} {exception?.StackTrace}");
                        return value;
                    }

                    return value;
                }

                // producing output with lazy calculations of greeks
                return new OptionPriceModelResult(npv,  // EvaluateOption ensure it is not NaN or Infinity
                            () => impliedVol.IsNaNOrInfinity() ? 0m : impliedVol.SafeDecimalCast(),
                            () => new Greeks(() => tryGetGreekOrReevaluate(() => option.delta(), (black) => black.delta(spot)),
                                            () => tryGetGreekOrReevaluate(() => option.gamma(), (black) => black.gamma(spot)),
                                            () => tryGetGreekOrReevaluate(() => option.vega(), (black) => black.vega(maturity)) / 100,   // per cent
                                            () => tryGetGreekOrReevaluate(() => option.theta(), (black) => black.theta(spot, maturity)),
                                            () => tryGetGreekOrReevaluate(() => option.rho(), (black) => black.rho(maturity)) / 100,        // per cent
                                            () => tryGetGreekOrReevaluate(() => option.elasticity(), (black) => black.elasticity(spot))));
            }
            catch (Exception err)
            {
                Log.Debug($"QLOptionPriceModel.Evaluate() error: {err.Message} {(Log.DebuggingEnabled ? err.StackTrace : string.Empty)} for {contract.Symbol}");
                return OptionPriceModelResult.None;
            }
        }

        /// <summary>
        /// Runs option evaluation and logs exceptions
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private static decimal EvaluateOption(VanillaOption option)
        {
            try
            {
                var npv = option.NPV();

                if (double.IsNaN(npv) ||
                    double.IsInfinity(npv))
                    return 0;

                // can return negative value in neighborhood of 0
                return Math.Max(0, npv).SafeDecimalCast();
            }
            catch (Exception err)
            {
                Log.Debug($"QLOptionPriceModel.EvaluateOption() error: {err.Message}");
                return 0;
            }
        }

        /// <summary>
        /// An implied volatility approximation by Newton-Raphson method. Return 0 if result is not converged
        /// </summary>
        /// <remarks>
        /// Orlando G, Taglialatela G. A review on implied volatility calculation. Journal of Computational and Applied Mathematics. 2017 Aug 15;320:202-20.
        /// https://www.sciencedirect.com/science/article/pii/S0377042717300602
        /// </remarks>
        /// <param name="price">current price of the option</param>
        /// <param name="initialGuess">initial guess of the IV</param>
        /// <param name="timeTillExpiry">time till option contract expiry</param>
        /// <param name="riskFreeDiscount">risk free rate discount factor</param>
        /// <param name="forwardPrice">future value of underlying price</param>
        /// <param name="payoff">payoff structure of the option contract</param>
        /// <param name="black">black calculator instance</param>
        /// <returns>implied volatility estimation</returns>
        protected double ImpliedVolatilityEstimation(double price, double initialGuess, double timeTillExpiry, double riskFreeDiscount, 
                                                     double forwardPrice, PlainVanillaPayoff payoff, out BlackCalculator black)
        {
            // Set up the optimizer
            const double tolerance = 1e-3d;
            const double lowerBound = 1e-7d;
            const double upperBound = 4d;
            var iterRemain = 10;
            var error = double.MaxValue;
            var impliedVolEstimate = initialGuess;

            // Set up option calculator
            black = CreateBlackCalculator(forwardPrice, riskFreeDiscount, initialGuess, payoff);

            while (error > tolerance && iterRemain > 0)
            {
                var oldImpliedVol = impliedVolEstimate;
                
                // Set up calculator by previous IV estimate to get new theoretical price, vega and IV
                black = CreateBlackCalculator(forwardPrice, riskFreeDiscount, oldImpliedVol, payoff);
                impliedVolEstimate -= (black.value() - price) / black.vega(timeTillExpiry);

                if (impliedVolEstimate < lowerBound)
                {
                    impliedVolEstimate = lowerBound;
                }
                else if (impliedVolEstimate > upperBound)
                {
                    impliedVolEstimate = upperBound;
                }

                error = Math.Abs(impliedVolEstimate - oldImpliedVol) / impliedVolEstimate;
                iterRemain--;
            }

            if (iterRemain == 0)
            {
                if (Log.DebuggingEnabled)
                {
                    Log.Debug("QLOptionPriceModel.ImpliedVolatilityEstimation() error: Implied Volatility approxiation did not converge, returning 0.");
                }
                return 0d;
            }

            return impliedVolEstimate;
        }

        /// <summary>
        /// Define Black Calculator to calculate Greeks that are not defined by the option object
        /// Some models do not evaluate all greeks under some circumstances (e.g. low dividend yield)
        /// We override this restriction to calculate the Greeks directly with the BlackCalculator
        /// </summary>
        private BlackCalculator CreateBlackCalculator(double forwardPrice, double riskFreeDiscount, double stdDev, PlainVanillaPayoff payoff)
        {
            return new BlackCalculator(payoff, forwardPrice, stdDev, riskFreeDiscount);
        }

        private static DateTime AddDays(DateTime date, int days, SecurityExchangeHours marketHours)
        {
            var forwardDate = date.AddDays(days);

            if (!marketHours.IsDateOpen(forwardDate))
            {
                forwardDate = marketHours.GetNextTradingDay(forwardDate);
            }

            return forwardDate;
        }

        /// <summary>
        /// Set the evaluation date
        /// </summary>
        /// <param name="evaluationDate">The current evaluation date</param>
        private void SetEvaluationDate(DateTime evaluationDate)
        {
            if (Settings.evaluationDate().ToDateTime() != evaluationDate)
            {
                Settings.setEvaluationDate(evaluationDate);
            }
        }
    }
}
