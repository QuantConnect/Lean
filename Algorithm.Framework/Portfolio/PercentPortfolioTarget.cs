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
    /// Provides an implmentation of <see cref="IPortfolioTarget"/> that is a percentage
    /// of the total portoflio value. This is useful for saying you'd like 10% of your portfolio
    /// in a particular security.
    /// </summary>
    public class PercentPortfolioTarget : IPortfolioTarget
    {
        /// <summary>
        /// Gets the symbol of this target
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the target percent of total portfolio value for the symbol
        /// </summary>
        public decimal Percent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PercentPortfolioTarget"/> class
        /// </summary>
        /// <param name="symbol">The symbol this target is for</param>
        /// <param name="percent">The percent of total portolio value</param>
        public PercentPortfolioTarget(Symbol symbol, decimal percent)
        {
            Symbol = symbol;
            Percent = percent;
        }

        /// <summary>
        /// Gets the quantity of this symbol the algorithm should hold
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <returns>The quantity of shares the algorithm should hold for this symbol</returns>
        public decimal GetTargetQuantity(QCAlgorithmFramework algorithm)
        {
            var security = algorithm.Securities[Symbol];
            if (security.Price == 0)
            {
                return 0;
            }

            var quantity = Percent * algorithm.Portfolio.TotalPortfolioValue / security.Price;

            // round down to nearest lot size
            var remainder = quantity % security.SymbolProperties.LotSize;
            return quantity - remainder;
        }
    }
}