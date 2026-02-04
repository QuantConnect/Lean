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

using QuantConnect.Securities.Option;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Provides option price models for option securities based on Lean's Greeks indicators
    /// </summary>
    public class IndicatorBasedOptionPriceModelProvider : IOptionPriceModelProvider
    {
        /// <summary>
        /// Gets the option price model for the specified option symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The option price model for the given symbol</returns>
        public IOptionPriceModel GetOptionPriceModel(Symbol symbol)
        {
            return new IndicatorBasedOptionPriceModel();
        }
    }
}
