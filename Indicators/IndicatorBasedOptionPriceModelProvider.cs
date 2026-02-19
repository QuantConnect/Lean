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

using QuantConnect.Data;
using QuantConnect.Securities.Option;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides option price models for option securities based on Lean's Greeks indicators
    /// </summary>
    public class IndicatorBasedOptionPriceModelProvider : IOptionPriceModelProvider
    {
        /// <summary>
        /// Singleton instance of the <see cref="IndicatorBasedOptionPriceModelProvider"/>
        /// </summary>
        public static IndicatorBasedOptionPriceModelProvider Instance { get; } = new();

        private IndicatorBasedOptionPriceModelProvider()
        {
        }

        /// <summary>
        /// Gets the option price model for the specified option symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <param name="pricingModelType">The option pricing model type to use</param>
        /// <returns>The option price model for the given symbol</returns>
        public IOptionPriceModel GetOptionPriceModel(Symbol symbol, OptionPricingModelType? pricingModelType = null)
        {
            return new IndicatorBasedOptionPriceModel(pricingModelType, pricingModelType);
        }

        /// <summary>
        /// Gets the option price model with the specified configuration
        /// </summary>
        /// <param name="optionModel">The option pricing model type to be used by the indicators</param>
        /// <param name="ivModel">The option pricing model type to be used by the implied volatility indicator</param>
        /// <param name="dividendYieldModel">The dividend yield model to be used by the indicators</param>
        /// <param name="riskFreeInterestRateModel">The risk free interest rate model to be used by the indicators</param>v
        /// <param name="useMirrorContract">Whether to use the mirror contract when possible</param>
        /// <returns>The option price model for the given symbol</returns>
        public IOptionPriceModel GetOptionPriceModel(OptionPricingModelType? optionModel = null,
            OptionPricingModelType? ivModel = null, IDividendYieldModel dividendYieldModel = null,
            IRiskFreeInterestRateModel riskFreeInterestRateModel = null,
            bool useMirrorContract = true)
        {
            return new IndicatorBasedOptionPriceModel(optionModel, ivModel, dividendYieldModel, riskFreeInterestRateModel, useMirrorContract);
        }
    }
}
