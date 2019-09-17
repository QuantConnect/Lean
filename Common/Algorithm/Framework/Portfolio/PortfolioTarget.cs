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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioTarget"/> that specifies a
    /// specified quantity of a security to be held by the algorithm
    /// </summary>
    public class PortfolioTarget : IPortfolioTarget
    {
        /// <summary>
        /// Gets the symbol of this target
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the target quantity for the symbol
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioTarget"/> class
        /// </summary>
        /// <param name="symbol">The symbol this target is for</param>
        /// <param name="quantity">The target quantity</param>
        public PortfolioTarget(Symbol symbol, decimal quantity)
        {
            Symbol = symbol;
            Quantity = quantity;
        }

        /// <summary>
        /// Creates a new target for the specified percent
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for getting total portfolio value and current security price</param>
        /// <param name="symbol">The symbol the target is for</param>
        /// <param name="percent">The requested target percent of total portfolio value</param>
        /// <returns>A portfolio target for the specified symbol/percent</returns>
        public static IPortfolioTarget Percent(IAlgorithm algorithm, Symbol symbol, double percent)
        {
            return Percent(algorithm, symbol, percent.SafeDecimalCast());
        }

        /// <summary>
        /// Creates a new target for the specified percent
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for getting total portfolio value and current security price</param>
        /// <param name="symbol">The symbol the target is for</param>
        /// <param name="percent">The requested target percent of total portfolio value</param>
        /// <param name="returnDeltaQuantity">True, result quantity will be the Delta required to reach target percent.
        /// False, the result quantity will be the Total quantity to reach the target percent, including current holdings</param>
        /// <returns>A portfolio target for the specified symbol/percent</returns>
        public static IPortfolioTarget Percent(IAlgorithm algorithm, Symbol symbol, decimal percent, bool returnDeltaQuantity = false)
        {
            var absolutePercentage = Math.Abs(percent);
            if (absolutePercentage > algorithm.Settings.MaxAbsolutePortfolioTargetPercentage
                || absolutePercentage != 0 && absolutePercentage < algorithm.Settings.MinAbsolutePortfolioTargetPercentage)
            {
                algorithm.Error(
                    Invariant($"The portfolio target percent: {percent}, does not comply with the current ") +
                    Invariant($"'Algorithm.Settings' 'MaxAbsolutePortfolioTargetPercentage': {algorithm.Settings.MaxAbsolutePortfolioTargetPercentage}") +
                    Invariant($" or 'MinAbsolutePortfolioTargetPercentage': {algorithm.Settings.MinAbsolutePortfolioTargetPercentage}. Skipping")
                );
                return null;
            }

            var security = algorithm.Securities[symbol];
            if (security.Price == 0)
            {
                algorithm.Error(Invariant(
                    $"The order quantity for {symbol.Value} cannot be calculated: the price of the security is zero."
                ));

                return null;
            }

            // Factoring in FreePortfolioValuePercentage.
            var adjustedPercent = percent * (1 - algorithm.Settings.FreePortfolioValuePercentage);

            var result = security.BuyingPowerModel.GetMaximumOrderQuantityForTargetValue(
                new GetMaximumOrderQuantityForTargetValueParameters(algorithm.Portfolio, security, adjustedPercent)
            );

            if (result.IsError)
            {
                algorithm.Error(Invariant(
                    $"Unable to compute order quantity of {symbol}. Reason: {result.Reason} Returning null."
                ));

                return null;
            }

            // be sure to back out existing holdings quantity since the buying power model yields
            // the required delta quantity to reach a final target portfolio value for a symbol
            var quantity = result.Quantity + (returnDeltaQuantity ? 0 : security.Holdings.Quantity);

            return new PortfolioTarget(symbol, quantity);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return $"{Symbol}: {Quantity.Normalize()}";
        }
    }
}