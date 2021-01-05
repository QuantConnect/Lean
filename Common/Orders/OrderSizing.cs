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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides methods for computing a maximum order size.
    /// </summary>
    public static class OrderSizing
    {
        /// <summary>
        /// Adjust the provided order size to respect maximum order size based on a percentage of current volume.
        /// </summary>
        /// <param name="security">The security object</param>
        /// <param name="maximumPercentCurrentVolume">The maximum percentage of the current bar's volume</param>
        /// <param name="desiredOrderSize">The desired order size to adjust</param>
        /// <returns>The signed adjusted order size</returns>
        public static decimal GetOrderSizeForPercentVolume(Security security, decimal maximumPercentCurrentVolume, decimal desiredOrderSize)
        {
            var maxOrderSize = maximumPercentCurrentVolume * security.Volume;
            var orderSize = Math.Min(maxOrderSize, Math.Abs(desiredOrderSize));

            return Math.Sign(desiredOrderSize) * AdjustByLotSize(security, orderSize);
        }

        /// <summary>
        /// Adjust the provided order size to respect the maximum total order value
        /// </summary>
        /// <param name="security">The security object</param>
        /// <param name="maximumOrderValueInAccountCurrency">The maximum order value in units of the account currency</param>
        /// <param name="desiredOrderSize">The desired order size to adjust</param>
        /// <returns>The signed adjusted order size</returns>
        public static decimal GetOrderSizeForMaximumValue(Security security, decimal maximumOrderValueInAccountCurrency, decimal desiredOrderSize)
        {
            var priceInAccountCurrency = security.Price
                                         * security.QuoteCurrency.ConversionRate
                                         * security.SymbolProperties.ContractMultiplier;

            if (priceInAccountCurrency == 0m)
            {
                return 0m;
            }

            var maxOrderSize =  maximumOrderValueInAccountCurrency / priceInAccountCurrency;
            var orderSize = Math.Min(maxOrderSize, Math.Abs(desiredOrderSize));

            return Math.Sign(desiredOrderSize) * AdjustByLotSize(security, orderSize);
        }

        /// <summary>
        /// Gets the remaining quantity to be ordered to reach the specified target quantity.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="target">The portfolio target</param>
        /// <returns>The signed remaining quantity to be ordered</returns>
        public static decimal GetUnorderedQuantity(IAlgorithm algorithm, IPortfolioTarget target)
        {
            var security = algorithm.Securities[target.Symbol];
            var holdings = security.Holdings.Quantity;
            var openOrderQuantity = algorithm.Transactions.GetOpenOrderTickets(target.Symbol)
                .Aggregate(0m, (d, t) => d + t.Quantity - t.QuantityFilled);
            var quantity = target.Quantity - holdings - openOrderQuantity;

            return AdjustByLotSize(security, quantity);
        }

        /// <summary>
        /// Adjusts the provided order quantity to respect the securities lot size.
        /// If the quantity is missing 1M part of the lot size it will be rounded up
        /// since we suppose it's due to floating point error, this is required to avoid diff
        /// between Py and C#
        /// </summary>
        /// <param name="security">The security instance</param>
        /// <param name="quantity">The desired quantity to adjust, can be signed</param>
        /// <returns>The signed adjusted quantity</returns>
        public static decimal AdjustByLotSize(Security security, decimal quantity)
        {
            var absQuantity = Math.Abs(quantity);
            // if the amount we are missing for +1 lot size is 1M part of a lot size
            // we suppose its due to floating point error and round up
            // Note: this is required to avoid a diff between Py and C# equivalent
            var remainder = absQuantity % security.SymbolProperties.LotSize;
            var missingForLotSize = security.SymbolProperties.LotSize - remainder;
            if (missingForLotSize < (security.SymbolProperties.LotSize / 1000000))
            {
                remainder -= security.SymbolProperties.LotSize;
            }
            absQuantity -= remainder;

            return absQuantity * Math.Sign(quantity);
        }
    }
}
