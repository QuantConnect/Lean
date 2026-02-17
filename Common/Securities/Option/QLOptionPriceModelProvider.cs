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

using QLNet;
using System;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Provides option price models for option securities based on QuantLib implementations
    /// </summary>
    public class QLOptionPriceModelProvider : IOptionPriceModelProvider
    {
        internal const int TimeStepsBinomial = 100;

        /// <summary>
        /// Singleton instance of the <see cref="QLOptionPriceModelProvider"/>
        /// </summary>
        public static QLOptionPriceModelProvider Instance { get; } = new();

        private QLOptionPriceModelProvider()
        {
        }

        /// <summary>
        /// Gets the option price model for the specified option symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The option price model for the given symbol</returns>
        public IOptionPriceModel GetOptionPriceModel(Symbol symbol)
        {
            return symbol.ID.OptionStyle switch
            {
                // CRR model has the best accuracy and speed suggested by
                // Branka, Zdravka & Tea (2014). Numerical Methods versus Bjerksund and Stensland Approximations for American Options Pricing.
                // International Journal of Economics and Management Engineering. 8:4.
                // Available via: https://downloads.dxfeed.com/specifications/dxLibOptions/Numerical-Methods-versus-Bjerksund-and-Stensland-Approximations-for-American-Options-Pricing-.pdf
                // Also refer to OptionPriceModelTests.MatchesIBGreeksBulk() test,
                // we select the most accurate and computational efficient model
                OptionStyle.American => OptionPriceModels.BinomialCoxRossRubinstein(),
                OptionStyle.European => OptionPriceModels.BlackScholes(),
                _ => throw new ArgumentException("Invalid OptionStyle")
            };
        }

        /// <summary>
        /// Pricing engine for European vanilla options using analytical formula.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_analytic_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public IOptionPriceModel BlackScholes()
        {
            return new QLOptionPriceModel(process => new AnalyticEuropeanEngine(process),
                allowedOptionStyles: [OptionStyle.European]);
        }

        /// <summary>
        /// Pricing engine for European and American vanilla options using binomial trees. Cox-Ross-Rubinstein(CRR) model.
        /// QuantLib reference: http://quantlib.org/reference/class_quant_lib_1_1_f_d_european_engine.html
        /// </summary>
        /// <returns>New option price model instance</returns>
        public IOptionPriceModel BinomialCoxRossRubinstein()
        {
            return new QLOptionPriceModel(process => new BinomialVanillaEngine<CoxRossRubinstein>(process, TimeStepsBinomial));
        }
    }
}
