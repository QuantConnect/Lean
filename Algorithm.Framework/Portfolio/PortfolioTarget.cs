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

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IPortfolioTarget"/>
    /// </summary>
    public static class PortfolioTarget
    {
        /// <summary>
        /// Creates a new target for the specified percent
        /// </summary>
        /// <param name="symbol">The symbol the target is for</param>
        /// <param name="percent">The requested target percent of total portfolio value</param>
        /// <returns>A portfolio target for the specified symbol/percent</returns>
        public static IPortfolioTarget Percent(Symbol symbol, decimal percent)
        {
            return new PercentPortfolioTarget(symbol, percent);
        }

        /// <summary>
        /// Creates a new target for the specified quantity
        /// </summary>
        /// <param name="symbol">The symbol the target is for</param>
        /// <param name="quantity">The requested target quantity</param>
        /// <returns>A portoflio target for the specified symbol/quantity</returns>
        public static IPortfolioTarget Quantity(Symbol symbol, decimal quantity)
        {
            return new QuantityPortfolioTarget(symbol, quantity);
        }
    }
}