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
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages
{
    public abstract class BaseOrderTestParameters
    {
        /// <summary>
        /// Calculates the adjusted limit price for an order based on its direction
        /// and a price adjustment factor, ensuring the price moves toward being filled.
        /// </summary>
        /// <param name="orderDirection">The direction of the order (Buy or Sell).</param>
        /// <param name="previousLimitPrice">The previous limit price of the order.</param>
        /// <param name="targetMarketPrice">The target market price used to adjust the limit price.</param>
        /// <param name="priceAdjustmentFactor">The factor by which the price is adjusted.</param>
        /// <returns>The new, adjusted limit price.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the order direction is not Buy or Sell.</exception>
        protected virtual decimal CalculateAdjustedLimitPrice(OrderDirection orderDirection, decimal previousLimitPrice, decimal targetMarketPrice, decimal priceAdjustmentFactor)
        {
            var adjustmentLimitPrice = orderDirection switch
            {
                OrderDirection.Buy => Math.Max(previousLimitPrice * priceAdjustmentFactor, targetMarketPrice * priceAdjustmentFactor),
                OrderDirection.Sell => Math.Min(previousLimitPrice / priceAdjustmentFactor, targetMarketPrice / priceAdjustmentFactor),
                _ => throw new NotSupportedException("Unsupported order direction: " + orderDirection)
            };
            Log.Trace($"{nameof(CalculateAdjustedLimitPrice)}: {orderDirection} | Prev: {previousLimitPrice}, Target: {targetMarketPrice}, AdjustmentFactor: {priceAdjustmentFactor}, Result: {adjustmentLimitPrice}");
            return adjustmentLimitPrice;
        }

        /// <summary>
        /// Rounds the given price to the nearest increment defined by the underlying symbol's minimum price variation.
        /// </summary>
        /// <param name="price">The original price to round.</param>
        /// <param name="minimumPriceVariation">The minimum tick size or price increment for the symbol.</param>
        /// <returns>The price rounded to the nearest valid increment.</returns>
        protected virtual decimal RoundPrice(decimal price, decimal minimumPriceVariation)
        {
            var roundOffPlaces = minimumPriceVariation.GetDecimalPlaces();
            var roundedPrice = Math.Round(price / roundOffPlaces) * roundOffPlaces;
            Log.Trace($"{nameof(BaseOrderTestParameters)}.{nameof(RoundPrice)}: Price = {price}, Minimum Price increment = {minimumPriceVariation}, Rounded price = {roundedPrice}");
            return roundedPrice;
        }

        protected void ApplyUpdateOrderRequests(IReadOnlyCollection<Order> orders, UpdateOrderFields fields)
        {
            foreach (var order in orders)
            {
                ApplyUpdateOrderRequest(order, fields);
            }
        }

        protected void ApplyUpdateOrderRequest(Order order, UpdateOrderFields fields)
        {
            order.ApplyUpdateOrderRequest(new UpdateOrderRequest(DateTime.UtcNow, order.Id, fields));
        }

        /// <summary>
        /// Base class for defining order test parameters.
        /// Implement <see cref="ToString"/> to provide a descriptive name
        /// for displaying the test case in <c>Visual Studio Test Explorer</c>.
        /// </summary>
        /// <returns>A string representing the test parameters for display purposes.</returns>
        public abstract override string ToString();
    }
}
