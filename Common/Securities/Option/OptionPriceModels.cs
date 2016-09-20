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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantLib;

namespace QuantConnect.Securities.Option
{
    using PricingEngineFuncEx = Func<Symbol, GeneralizedBlackScholesProcess, PricingEngine>;

    /// <summary>
    /// Static class contains definitions of major option pricing models that can be used in LEAN
    /// </summary>
    public static class OptionPriceModels
    {
        /// <summary>
        /// Pricing engine for European vanilla options using analytical formulae. 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_analytic_european_engine.html
        /// </summary>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BlackScholes(IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new AnalyticEuropeanEngine(process),
                                           underlyingVolEstimator,
                                           riskFreeRateEstimator,
                                           dividendYieldEstimator);
        }

        /// <summary>
        /// Barone-Adesi and Whaley pricing engine for American options (1987)
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_barone_adesi_whaley_approximation_engine.html
        /// </summary>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BaroneAdesiWhaley(IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BaroneAdesiWhaleyEngine(process),
                                           underlyingVolEstimator,
                                           riskFreeRateEstimator,
                                           dividendYieldEstimator);
        }

        /// <summary>
        /// Bjerksund and Stensland pricing engine for American options (1993) 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_bjerksund_stensland_approximation_engine.html
        /// </summary>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BjerksundStensland(IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BjerksundStenslandEngine(process),
                                           underlyingVolEstimator,
                                           riskFreeRateEstimator,
                                           dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for European vanilla options using integral approach. 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_integral_engine.html
        /// </summary>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel Integral(IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new IntegralEngine(process),
                                           underlyingVolEstimator,
                                           riskFreeRateEstimator,
                                           dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for European options using finite-differences. 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel CrankNicolsonFD(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            PricingEngineFuncEx pricingEngineFunc = (symbol, process) =>
                            symbol.ID.OptionStyle == OptionStyle.American ?
                            new FDAmericanEngine(process, (uint)timeSteps, (uint)timeSteps - 1) as PricingEngine:
                            new FDEuropeanEngine(process, (uint)timeSteps, (uint)timeSteps - 1) as PricingEngine;

            return new QLOptionPriceModel(pricingEngineFunc,
                                           underlyingVolEstimator,
                                           riskFreeRateEstimator,
                                           dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Jarrow-Rudd model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialJarrowRudd(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "jarrowrudd", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }


        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Cox-Ross-Rubinstein(CRR) model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialCoxRossRubinstein(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "coxrossrubinstein", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Additive Equiprobabilities model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel AdditiveEquiprobabilities(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "eqp", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Trigeorgis model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialTrigeorgis(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "trigeorgis", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Tian model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialTian(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "tian", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Leisen-Reimer model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialLeisenReimer(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "leisenreimer", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Joshi model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <param name="timeSteps">Number of steps in binomial tree</param>
        /// <param name="underlyingVolEstimator">The estimator of underlying volatility (or null for default)</param>
        /// <param name="riskFreeRateEstimator">The estimator of risk free rate (or null for default)</param>
        /// <param name="dividendYieldEstimator">The estimator of stock dividend yield (or null for default)</param>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialJoshi(int timeSteps = 201, IUnderlyingVolatilityEstimator underlyingVolEstimator = null, IRiskFreeRateEstimator riskFreeRateEstimator = null, IDividendYieldEstimator dividendYieldEstimator = null)
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine(process, "joshi4", (uint)timeSteps),
                                          underlyingVolEstimator,
                                          riskFreeRateEstimator,
                                          dividendYieldEstimator);
        }

    }
}
