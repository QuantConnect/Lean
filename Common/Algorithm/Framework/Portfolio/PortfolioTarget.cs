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
using System.Collections.Generic;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioTarget"/> that specifies a
    /// specified quantity of a security to be held by the algorithm
    /// </summary>
    public class PortfolioTarget : IPortfolioTarget
    {

        /// <summary>
        /// Flag to determine if the minimum order margin portfolio percentage warning should or has already been sent to the user algorithm
        /// <see cref="IAlgorithmSettings.MinimumOrderMarginPortfolioPercentage"/>
        /// </summary>
        public static bool? MinimumOrderMarginPercentageWarningSent { get; set; }

        /// <summary>
        /// Gets the symbol of this target
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// Gets the target quantity for the symbol
        /// </summary>
        public decimal Quantity { get; }

        /// <summary>
        /// Portfolio target tag with additional information
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioTarget"/> class
        /// </summary>
        /// <param name="symbol">The symbol this target is for</param>
        /// <param name="quantity">The target quantity</param>
        /// <param name="tag">The target tag with additional information</param>
        public PortfolioTarget(Symbol symbol, decimal quantity, string tag = "")
        {
            Symbol = symbol;
            Quantity = quantity;
            Tag = tag;
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
        /// <param name="tag">The target tag with additional information</param>
        /// <returns>A portfolio target for the specified symbol/percent</returns>
        public static IPortfolioTarget Percent(IAlgorithm algorithm, Symbol symbol, double percent, string tag)
        {
            return Percent(algorithm, symbol, percent.SafeDecimalCast(), tag: tag);
        }

        /// <summary>
        /// Creates a new target for the specified percent
        /// </summary>
        /// <param name="algorithm">The algorithm instance, used for getting total portfolio value and current security price</param>
        /// <param name="symbol">The symbol the target is for</param>
        /// <param name="percent">The requested target percent of total portfolio value</param>
        /// <param name="returnDeltaQuantity">True, result quantity will be the Delta required to reach target percent.
        /// False, the result quantity will be the Total quantity to reach the target percent, including current holdings</param>
        /// <param name="tag">The target tag with additional information</param>
        /// <returns>A portfolio target for the specified symbol/percent</returns>
        public static IPortfolioTarget Percent(IAlgorithm algorithm, Symbol symbol, decimal percent, bool returnDeltaQuantity = false, string tag = "")
        {
            var absolutePercentage = Math.Abs(percent);
            if (absolutePercentage > algorithm.Settings.MaxAbsolutePortfolioTargetPercentage
                || absolutePercentage != 0 && absolutePercentage < algorithm.Settings.MinAbsolutePortfolioTargetPercentage)
            {
                algorithm.Error(Messages.PortfolioTarget.InvalidTargetPercent(algorithm, percent));
                return null;
            }

            Security security;
            try
            {
                security = algorithm.Securities[symbol];
            }
            catch (KeyNotFoundException)
            {
                algorithm.Error(Messages.PortfolioTarget.SymbolNotFound(symbol));
                return null;
            }

            if (security.Price == 0)
            {
                algorithm.Error(symbol.GetZeroPriceMessage());
                return null;
            }

            // Factoring in FreePortfolioValuePercentage.
            var adjustedPercent = percent * algorithm.Portfolio.TotalPortfolioValueLessFreeBuffer / algorithm.Portfolio.TotalPortfolioValue;

            // we normalize the target buying power by the leverage so we work in the land of margin
            var targetFinalMarginPercentage = adjustedPercent / security.BuyingPowerModel.GetLeverage(security);

            var positionGroup = algorithm.Portfolio.Positions.GetOrCreateDefaultGroup(security);
            var result = positionGroup.BuyingPowerModel.GetMaximumLotsForTargetBuyingPower(
                new GetMaximumLotsForTargetBuyingPowerParameters(algorithm.Portfolio, positionGroup,
                    targetFinalMarginPercentage, algorithm.Settings.MinimumOrderMarginPortfolioPercentage));

            if (result.IsError)
            {
                algorithm.Error(Messages.PortfolioTarget.UnableToComputeOrderQuantityDueToNullResult(symbol, result));

                return null;
            }

            if (MinimumOrderMarginPercentageWarningSent.HasValue && !MinimumOrderMarginPercentageWarningSent.Value)
            {
                // we send the warning once
                MinimumOrderMarginPercentageWarningSent = true;
                algorithm.Debug(Messages.BuyingPowerModel.TargetOrderMarginNotAboveMinimum());
            }

            // be sure to back out existing holdings quantity since the buying power model yields
            // the required delta quantity to reach a final target portfolio value for a symbol
            var lotSize = security.SymbolProperties.LotSize;
            var quantity = result.NumberOfLots * lotSize + (returnDeltaQuantity ? 0 : security.Holdings.Quantity);

            return new PortfolioTarget(symbol, quantity, tag);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Messages.PortfolioTarget.ToString(this);
        }
    }
}
