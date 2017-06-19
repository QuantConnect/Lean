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
using QLNet;

namespace QuantConnect.Securities.Option
{
    using PricingEngineFuncEx = Func<Symbol, GeneralizedBlackScholesProcess, IPricingEngine>;

    /// <summary>
    /// Static class contains definitions of major option pricing models that can be used in LEAN
    /// </summary>
    /// <remarks>
    /// To introduce particular model into algorithm add the following line to the algorithm's Initialize() method: 
    ///     
    ///     option.PriceModel = OptionPriceModels.BjerksundStensland(); // Option pricing model of choice
    /// 
    /// </remarks>
    public static class OptionPriceModels
    {
        private static IQLUnderlyingVolatilityEstimator _underlyingVolEstimator = new ConstantQLUnderlyingVolatilityEstimator();
        private static IQLRiskFreeRateEstimator _riskFreeRateEstimator = new ConstantQLRiskFreeRateEstimator();
        private static IQLDividendYieldEstimator _dividendYieldEstimator = new ConstantQLDividendYieldEstimator();

        private const int _timeStepsBinomial = 100;
        private const int _timeStepsFD = 100;

        /// <summary>
        /// Pricing engine for European vanilla options using analytical formulae. 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_analytic_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BlackScholes()
        {
            return new QLOptionPriceModel(process => new AnalyticEuropeanEngine(process),
                                           _underlyingVolEstimator,
                                           _riskFreeRateEstimator,
                                           _dividendYieldEstimator);
        }

        /// <summary>
        /// Barone-Adesi and Whaley pricing engine for American options (1987)
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_barone_adesi_whaley_approximation_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BaroneAdesiWhaley()
        {
            return new QLOptionPriceModel(process => new BaroneAdesiWhaleyApproximationEngine(process),
                                           _underlyingVolEstimator,
                                           _riskFreeRateEstimator,
                                           _dividendYieldEstimator);
        }

        /// <summary>
        /// Bjerksund and Stensland pricing engine for American options (1993) 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_bjerksund_stensland_approximation_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BjerksundStensland()
        {
            return new QLOptionPriceModel(process => new BjerksundStenslandApproximationEngine(process),
                                           _underlyingVolEstimator,
                                           _riskFreeRateEstimator,
                                           _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for European vanilla options using integral approach. 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_integral_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel Integral()
        {
            return new QLOptionPriceModel(process => new IntegralEngine(process),
                                           _underlyingVolEstimator,
                                           _riskFreeRateEstimator,
                                           _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for European options using finite-differences. 
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel CrankNicolsonFD()
        {
            PricingEngineFuncEx pricingEngineFunc = (symbol, process) =>
                            symbol.ID.OptionStyle == OptionStyle.American ?
                            new FDAmericanEngine(process, _timeStepsFD, _timeStepsFD - 1) as IPricingEngine:
                            new FDEuropeanEngine(process, _timeStepsFD, _timeStepsFD - 1) as IPricingEngine;

            return new QLOptionPriceModel(pricingEngineFunc,
                                           _underlyingVolEstimator,
                                           _riskFreeRateEstimator,
                                           _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Jarrow-Rudd model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialJarrowRudd()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<JarrowRudd>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }


        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Cox-Ross-Rubinstein(CRR) model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialCoxRossRubinstein()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<CoxRossRubinstein>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Additive Equiprobabilities model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel AdditiveEquiprobabilities()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<AdditiveEQPBinomialTree>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Trigeorgis model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialTrigeorgis()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<Trigeorgis>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Tian model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialTian()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<Tian>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Leisen-Reimer model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialLeisenReimer()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<LeisenReimer>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }

        /// <summary>
        /// Pricing engine for vanilla options using binomial trees. Joshi model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialJoshi()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<Joshi4>(process, _timeStepsBinomial),
                                          _underlyingVolEstimator,
                                          _riskFreeRateEstimator,
                                          _dividendYieldEstimator);
        }

    }
}
