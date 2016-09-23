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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QLNet;
using QuantConnect.Util;

namespace QuantConnect.Securities.Option
{
    using PricingEngineFunc = Func<GeneralizedBlackScholesProcess, IPricingEngine>;
    using PricingEngineFuncEx = Func<Symbol, GeneralizedBlackScholesProcess, IPricingEngine>;

    /// <summary>
    /// Provides QuantLib(QL) implementation of <see cref="IOptionPriceModel"/> to support major option pricing models, available in QL. 
    /// </summary>
    class QLOptionPriceModel : IOptionPriceModel
    {
        private readonly IQLUnderlyingVolatilityEstimator _underlyingVolEstimator;
        private readonly IQLRiskFreeRateEstimator _riskFreeRateEstimator;
        private readonly IQLDividendYieldEstimator _dividendYieldEstimator;
        private readonly PricingEngineFuncEx _pricingEngineFunc;

        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        public QLOptionPriceModel(PricingEngineFunc pricingEngineFunc, IQLUnderlyingVolatilityEstimator underlyingVolEstimator, IQLRiskFreeRateEstimator riskFreeRateEstimator, IQLDividendYieldEstimator dividendYieldEstimator)
        {
            _pricingEngineFunc = (option, process) => pricingEngineFunc(process);
            _underlyingVolEstimator = underlyingVolEstimator ?? new DefaultQLUnderlyingVolatilityEstimator();
            _riskFreeRateEstimator = riskFreeRateEstimator ?? new DefaultQLRiskFreeRateEstimator();
            _dividendYieldEstimator = dividendYieldEstimator ?? new DefaultQLDividendYieldEstimator();
        }
        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function takes option and modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        public QLOptionPriceModel(PricingEngineFuncEx pricingEngineFunc, IQLUnderlyingVolatilityEstimator underlyingVolEstimator, IQLRiskFreeRateEstimator riskFreeRateEstimator, IQLDividendYieldEstimator dividendYieldEstimator)
        {
            _pricingEngineFunc = pricingEngineFunc;
            _underlyingVolEstimator = underlyingVolEstimator ?? new DefaultQLUnderlyingVolatilityEstimator();
            _riskFreeRateEstimator = riskFreeRateEstimator ?? new DefaultQLRiskFreeRateEstimator();
            _dividendYieldEstimator = dividendYieldEstimator ?? new DefaultQLDividendYieldEstimator();
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
            // setting up option pricing parameters
            var optionSecurity = (Option)security;
            var settlementDate = contract.Time.Date.AddDays(Option.DefaultSettlementDays);
            var maturityDate = contract.Expiry.Date.AddDays(Option.DefaultSettlementDays);
            var underlyingQuote = new Handle<Quote>(new SimpleQuote((double)optionSecurity.Underlying.Close));
            var dividendYield = _dividendYieldEstimator.Estimate(security, slice, contract);
            var riskFreeRate = _riskFreeRateEstimator.Estimate(security, slice, contract);
            var underlyingVol = _underlyingVolEstimator.Estimate(security, slice, contract);

            if (underlyingVol == null ||
                riskFreeRate == null ||
                dividendYield == null)
            {
                return new OptionPriceModelResult(0.0m, new Greeks());
            }

            // preparing stochastic process and payoff functions
            var stochasticProcess = new BlackScholesMertonProcess(underlyingQuote, dividendYield, riskFreeRate, underlyingVol);
            var payoff = new PlainVanillaPayoff(contract.Right == OptionRight.Call? QLNet.Option.Type.Call : QLNet.Option.Type.Put, (double)contract.Strike);

            // creating option QL object
            var option = contract.Symbol.ID.OptionStyle == OptionStyle.American? 
                        new VanillaOption(payoff, new AmericanExercise(settlementDate, maturityDate)):
                        new VanillaOption(payoff, new EuropeanExercise(maturityDate));

            Settings.setEvaluationDate(settlementDate);

            // preparing pricing engine QL object
            option.setPricingEngine(_pricingEngineFunc(contract.Symbol, stochasticProcess));

            // running calculations
            var theoreticalPrice = option.NPV();

            // function extracts QL greeks catching exception if greek is not generated by the pricing engine
            Func<Func<double>, decimal> tryGetGreek = greek =>
            {
                try
                {
                    return (decimal)greek();
                }
                catch(Exception)
                {
                    return 0m;
                }
            };

            // producing output with lazy calculations of IV and greeks

            return new OptionPriceModelResult((decimal)theoreticalPrice,
                        () => (decimal)option.impliedVolatility((double)optionSecurity.Close, stochasticProcess),
                        () => new Greeks(tryGetGreek(() => option.delta()),
                                        tryGetGreek(() => option.gamma()),
                                        tryGetGreek(() => option.vega()),
                                        tryGetGreek(() => option.theta()),
                                        tryGetGreek(() => option.rho()),
                                        0m));
        }
    }
}
