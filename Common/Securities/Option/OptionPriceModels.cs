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

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Static class contains definitions of major option pricing models that can be used in LEAN
    /// </summary>
    /// <remarks>
    /// To introduce particular model into algorithm add the following line to the algorithm's Initialize() method:
    ///
    ///     option.PriceModel = OptionPriceModels.BlackScholes(); // Option pricing model of choice
    ///
    /// </remarks>
    public static partial class OptionPriceModels
    {
        /// <summary>
        /// Default option price model provider used by LEAN when creating price models.
        /// </summary>
        internal static IOptionPriceModelProvider DefaultPriceModelProvider { get; set; }

        /// <summary>
        /// Null pricing engine that returns the current price as the option theoretical price.
        /// It will also set the option Greeks and implied volatility to zero, effectively disabling the pricing.
        /// </summary>
        public static IOptionPriceModel Null()
        {
            return new CurrentPriceOptionPriceModel();
        }

        /// <summary>
        /// Pricing engine for Black-Scholes model.
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BlackScholes()
        {
            return DefaultPriceModelProvider.GetOptionPriceModel(Symbol.Empty, Indicators.OptionPricingModelType.BlackScholes);
        }

        /// <summary>
        /// Pricing engine for Cox-Ross-Rubinstein (CRR) model.
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel BinomialCoxRossRubinstein()
        {
            return DefaultPriceModelProvider.GetOptionPriceModel(Symbol.Empty, Indicators.OptionPricingModelType.BinomialCoxRossRubinstein);
        }

        /// <summary>
        /// Pricing engine for forward binomial tree model.
        /// </summary>
        /// <returns>New option price model instance</returns>
        public static IOptionPriceModel ForwardTree()
        {
            return DefaultPriceModelProvider.GetOptionPriceModel(Symbol.Empty, Indicators.OptionPricingModelType.ForwardTree);
        }
    }
}
