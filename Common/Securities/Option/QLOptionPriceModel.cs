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

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QLNet;

namespace QuantConnect.Securities.Option
{
    using Logging;
    using PricingEngineFunc = Func<GeneralizedBlackScholesProcess, IPricingEngine>;
    using PricingEngineFuncEx = Func<Symbol, GeneralizedBlackScholesProcess, IPricingEngine>;

    /// <summary>
    /// Provides QuantLib(QL) implementation of <see cref="IOptionPriceModel"/> to support major option pricing models, available in QL.
    /// </summary>
    public class QLOptionPriceModel : IOptionPriceModel
    {
        private static readonly OptionStyle[] _defaultAllowedOptionStyles = new[] { OptionStyle.European, OptionStyle.American };

        private readonly IQLUnderlyingVolatilityEstimator _underlyingVolEstimator;
        private readonly IQLRiskFreeRateEstimator _riskFreeRateEstimator;
        private readonly IQLDividendYieldEstimator _dividendYieldEstimator;
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
        public OptionStyle[] AllowedOptionStyles { get; }

        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        /// <param name="allowedOptionStyles">List of option styles supported by the pricing model. It defaults to both American and European option styles</param>
        public QLOptionPriceModel(PricingEngineFunc pricingEngineFunc, IQLUnderlyingVolatilityEstimator underlyingVolEstimator, IQLRiskFreeRateEstimator riskFreeRateEstimator, IQLDividendYieldEstimator dividendYieldEstimator, OptionStyle[] allowedOptionStyles = null)
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
        public QLOptionPriceModel(PricingEngineFuncEx pricingEngineFunc, IQLUnderlyingVolatilityEstimator underlyingVolEstimator, IQLRiskFreeRateEstimator riskFreeRateEstimator, IQLDividendYieldEstimator dividendYieldEstimator, OptionStyle[] allowedOptionStyles = null)
        {
            _pricingEngineFunc = pricingEngineFunc;
            _underlyingVolEstimator = underlyingVolEstimator ?? new ConstantQLUnderlyingVolatilityEstimator();
            _riskFreeRateEstimator = riskFreeRateEstimator ?? new ConstantQLRiskFreeRateEstimator();
            _dividendYieldEstimator = dividendYieldEstimator ?? new ConstantQLDividendYieldEstimator();

            AllowedOptionStyles = allowedOptionStyles ?? _defaultAllowedOptionStyles;

            // Required for QL to consider the option as not expired on the expiration date.
            // This is done in the constructor instead of a static contructor because QLNet.Settings attributes are ThreadStatic,
            // so doing it in a static constructor would only set it for the first thread that instantiates QLOptionPriceModel or
            // accesses any of its static members.
            QLNet.Settings.includeReferenceDateEvents = true;
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
                    return OptionPriceModelResult.None;
                }

                // setting up option pricing parameters
                var calendar = new UnitedStates();
                var dayCounter = new Actual365Fixed();
                var optionSecurity = (Option)security;

                var securityExchangeHours = security.Exchange.Hours;
                var settlementDate = AddDays(contract.Time.Date, Option.DefaultSettlementDays, securityExchangeHours);
                var maturityDate = AddDays(contract.Expiry.Date, Option.DefaultSettlementDays, securityExchangeHours);
                var underlyingQuoteValue = new SimpleQuote((double)optionSecurity.Underlying.Price);

                var dividendYieldValue = new SimpleQuote(_dividendYieldEstimator.Estimate(security, slice, contract));
                var dividendYield = new Handle<YieldTermStructure>(new FlatForward(0, calendar, dividendYieldValue, dayCounter));

                var riskFreeRateValue = new SimpleQuote(_riskFreeRateEstimator.Estimate(security, slice, contract));
                var riskFreeRate = new Handle<YieldTermStructure>(new FlatForward(0, calendar, riskFreeRateValue, dayCounter));

                var underlyingVolValue = new SimpleQuote(_underlyingVolEstimator.Estimate(security, slice, contract));
                var underlyingVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(0, calendar, new Handle<Quote>(underlyingVolValue), dayCounter));

                if (!_underlyingVolEstimator.IsReady)
                {
                    return OptionPriceModelResult.None;
                }

                // preparing stochastic process and payoff functions
                var stochasticProcess = new BlackScholesMertonProcess(new Handle<Quote>(underlyingQuoteValue), dividendYield, riskFreeRate, underlyingVol);
                var payoff = new PlainVanillaPayoff(contract.Right == OptionRight.Call ? QLNet.Option.Type.Call : QLNet.Option.Type.Put, (double)contract.Strike);

                // creating option QL object
                var option = contract.Symbol.ID.OptionStyle == OptionStyle.American ?
                            new VanillaOption(payoff, new AmericanExercise(settlementDate, maturityDate)) :
                            new VanillaOption(payoff, new EuropeanExercise(maturityDate));

                QLNet.Settings.setEvaluationDate(settlementDate);

                // preparing pricing engine QL object
                option.setPricingEngine(_pricingEngineFunc(contract.Symbol, stochasticProcess));

                // running calculations
                // can return negative value in neighbourhood of 0
                var npv = Math.Max(0, EvaluateOption(option));

                // function extracts QL greeks catching exception if greek is not generated by the pricing engine and reevaluates option to get numerical estimate of the seisitivity
                Func<Func<double>, Func<double>, decimal> tryGetGreekOrReevaluate = (greek, reevalFunc) =>
                {
                    try
                    {
                        return (decimal)greek();
                    }
                    catch (Exception)
                    {
                        return EnableGreekApproximation ? (decimal)reevalFunc() : 0.0m;
                    }
                };

                // function extracts QL greeks catching exception if greek is not generated by the pricing engine
                Func<Func<double>, decimal> tryGetGreek = greek => tryGetGreekOrReevaluate(greek, () => 0.0);

                // function extracts QL IV catching exception if IV is not generated by the pricing engine
                Func<decimal> tryGetImpliedVol = () =>
                {
                    try
                    {
                        return (decimal)option.impliedVolatility((double)optionSecurity.Price, stochasticProcess);
                    }
                    catch (Exception err)
                    {
                        Log.Debug($"tryGetImpliedVol() error: {err.Message}");
                        return 0m;
                    }
                };

                Func<Tuple<decimal, decimal>> evalDeltaGamma = () =>
                {
                    try
                    {
                        return Tuple.Create((decimal)option.delta(), (decimal)option.gamma());
                    }
                    catch (Exception)
                    {
                        if (EnableGreekApproximation)
                        {
                            var step = 0.01;
                            var initial = underlyingQuoteValue.value();
                            underlyingQuoteValue.setValue(initial - step);
                            var npvMinus = EvaluateOption(option);
                            underlyingQuoteValue.setValue(initial + step);
                            var npvPlus = EvaluateOption(option);
                            underlyingQuoteValue.setValue(initial);

                            return Tuple.Create((decimal)((npvPlus - npvMinus) / (2 * step)),
                                                (decimal)((npvPlus - 2 * npv + npvMinus) / (step * step)));
                        }
                        else
                            return Tuple.Create(0.0m, 0.0m);
                    }
                };

                Func<double> reevalVega = () =>
                {
                    var step = 0.001;
                    var initial = underlyingVolValue.value();
                    underlyingVolValue.setValue(initial + step);
                    var npvPlus = EvaluateOption(option);
                    underlyingVolValue.setValue(initial);

                    return (npvPlus - npv) / step;
                };

                Func<double> reevalTheta = () =>
                {
                    var step = 1.0 / 365.0;

                    Settings.setEvaluationDate(security.Exchange.Hours.GetPreviousTradingDay(settlementDate));
                    var npvMinus = EvaluateOption(option);
                    QLNet.Settings.setEvaluationDate(settlementDate);

                    return (npv - npvMinus) / step;
                };

                Func<double> reevalRho = () =>
                {
                    var step = 0.001;
                    var initial = riskFreeRateValue.value();
                    riskFreeRateValue.setValue(initial + step);
                    var npvPlus = EvaluateOption(option);
                    riskFreeRateValue.setValue(initial);

                    return (npvPlus - npv) / step;
                };

                // producing output with lazy calculations of IV and greeks

                return new OptionPriceModelResult((decimal)npv,
                            tryGetImpliedVol,
                            () => new Greeks(evalDeltaGamma,
                                            () => tryGetGreekOrReevaluate(() => option.vega(), reevalVega),
                                            () => tryGetGreekOrReevaluate(() => option.theta(), reevalTheta),
                                            () => tryGetGreekOrReevaluate(() => option.rho(), reevalRho),
                                            () => tryGetGreek(() => option.elasticity())));
            }
            catch (Exception err)
            {
                Log.Debug($"QLOptionPriceModel.Evaluate() error: {err.Message}");
                return OptionPriceModelResult.None;
            }
        }

        /// <summary>
        /// Runs option evaluation and logs exceptions
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private static double EvaluateOption(VanillaOption option)
        {
            try
            {
                var npv = option.NPV();

                if (double.IsNaN(npv) ||
                    double.IsInfinity(npv))
                    npv = 0.0;

                return npv;
            }
            catch (Exception err)
            {
                Log.Debug($"QLOptionPriceModel.EvaluateOption() error: {err.Message}");
                return 0.0;
            }
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
    }
}
