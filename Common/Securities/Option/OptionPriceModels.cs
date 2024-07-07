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
using System.Linq;
using Fasterflect;
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
        private const int _timeStepsBinomial = 100;
        private const int _timeStepsFD = 100;

        /// <summary>
        /// Creates pricing engine by engine type name.
        /// </summary>
        /// <param name="priceEngineName">QL price engine name</param>
        /// <param name="riskFree">The risk free rate</param>
        /// <param name="allowedOptionStyles">List of option styles supported by the pricing model. It defaults to both American and European option styles</param>
        /// <returns>New option price model instance of specific engine</returns>
        public static IOptionPriceModel Create(
            string priceEngineName,
            decimal riskFree,
            OptionStyle[] allowedOptionStyles = null
        )
        {
            var type = AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .Where(s => s.Implements(typeof(IPricingEngine)))
                .FirstOrDefault(t =>
                    t.FullName?.EndsWith(priceEngineName, StringComparison.InvariantCulture) == true
                );

            return new QLOptionPriceModel(
                process => (IPricingEngine)Activator.CreateInstance(type, process),
                riskFreeRateEstimator: new ConstantQLRiskFreeRateEstimator(riskFree),
                allowedOptionStyles: allowedOptionStyles
            );
        }

        /// <summary>
        /// Pricing engine for European vanilla options using analytical formula.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_analytic_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BlackScholes()
        {
            return new QLOptionPriceModel(
                process => new AnalyticEuropeanEngine(process),
                allowedOptionStyles: new[] { OptionStyle.European }
            );
        }

        /// <summary>
        /// Barone-Adesi and Whaley pricing engine for American options (1987)
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_barone_adesi_whaley_approximation_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BaroneAdesiWhaley()
        {
            return new QLOptionPriceModel(
                process => new BaroneAdesiWhaleyApproximationEngine(process),
                allowedOptionStyles: new[] { OptionStyle.American }
            );
        }

        /// <summary>
        /// Bjerksund and Stensland pricing engine for American options (1993)
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_bjerksund_stensland_approximation_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BjerksundStensland()
        {
            return new QLOptionPriceModel(
                process => new BjerksundStenslandApproximationEngine(process),
                allowedOptionStyles: new[] { OptionStyle.American }
            );
        }

        /// <summary>
        /// Pricing engine for European vanilla options using integral approach.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_integral_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel Integral()
        {
            return new QLOptionPriceModel(
                process => new IntegralEngine(process),
                allowedOptionStyles: new[] { OptionStyle.European }
            );
        }

        /// <summary>
        /// Pricing engine for European and American options using finite-differences.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel CrankNicolsonFD()
        {
            PricingEngineFuncEx pricingEngineFunc = (symbol, process) =>
                symbol.ID.OptionStyle == OptionStyle.American
                    ? new FDAmericanEngine(process, _timeStepsFD, _timeStepsFD - 1)
                    : new FDEuropeanEngine(process, _timeStepsFD, _timeStepsFD - 1);

            return new QLOptionPriceModel(pricingEngineFunc);
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Jarrow-Rudd model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialJarrowRudd()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<JarrowRudd>(
                process,
                _timeStepsBinomial
            ));
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Cox-Ross-Rubinstein(CRR) model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialCoxRossRubinstein()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<CoxRossRubinstein>(
                process,
                _timeStepsBinomial
            ));
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Additive Equiprobabilities model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel AdditiveEquiprobabilities()
        {
            return new QLOptionPriceModel(
                process => new BinomialVanillaEngine<AdditiveEQPBinomialTree>(
                    process,
                    _timeStepsBinomial
                )
            );
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Trigeorgis model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialTrigeorgis()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<Trigeorgis>(
                process,
                _timeStepsBinomial
            ));
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Tian model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialTian()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<Tian>(
                process,
                _timeStepsBinomial
            ));
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Leisen-Reimer model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialLeisenReimer()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<LeisenReimer>(
                process,
                _timeStepsBinomial
            ));
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Joshi model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialJoshi()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<Joshi4>(
                process,
                _timeStepsBinomial
            ));
        }
    }
}
