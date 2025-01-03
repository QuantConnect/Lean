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

using Newtonsoft.Json;
using QuantConnect.Interfaces;
using System;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Trailing Stop Limit Order Type Definition
    /// </summary>
    public class TrailingStopLimitOrder : StopLimitOrder
    {
        /// <summary>
        /// Trailing amount for this trailing stop limit order
        /// </summary>
        [JsonProperty(PropertyName = "trailingAmount")]
        public decimal TrailingAmount { get; internal set; }

        /// <summary>
        /// Determines whether the <see cref="TrailingAmount"/> is a percentage or an absolute currency value
        /// </summary>
        [JsonProperty(PropertyName = "trailingAsPercentage")]
        public bool TrailingAsPercentage { get; internal set; }

        /// <summary>
        /// Limit offset amount for this trailing stop limit order
        /// </summary>
        [JsonProperty(PropertyName = "limitOffset")]
        public decimal LimitOffset { get; internal set; }

        /// <summary>
        /// TrailingStopLimit Order Type
        /// </summary>
        public override OrderType Type
        {
            get { return OrderType.TrailingStopLimit; }
        }

        /// <summary>
        /// Default constructor for JSON Deserialization:
        /// </summary>
        public TrailingStopLimitOrder()
        {
        }

        /// <summary>
        /// New Trailing Stop Limit Order constructor
        /// </summary>
        /// <param name="symbol">Symbol of the asset being traded</param>
        /// <param name="quantity">Quantity of the asset being traded</param>
        /// <param name="stopPrice">Initial stop price at which the order should be triggered</param>
        /// <param name="limitPrice">Price the order should be filled at if triggered</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="limitOffset">The limit offset amount used to update the limit price</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The properties for this order</param>
        public TrailingStopLimitOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, decimal trailingAmount, bool trailingAsPercentage,
            decimal limitOffset, DateTime time, string tag = "", IOrderProperties properties = null)
            : base(symbol, quantity, stopPrice, limitPrice, time, tag, properties)
        {
            TrailingAmount = trailingAmount;
            TrailingAsPercentage = trailingAsPercentage;
            LimitOffset = limitOffset;
        }

        /// <summary>
        /// Gets the default tag for this order
        /// </summary>
        /// <returns>The default tag</returns>
        public override string GetDefaultTag()
        {
            return Messages.TrailingStopLimitOrder.Tag(this);
        }

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public override void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            base.ApplyUpdateOrderRequest(request);
            if (request.TrailingAmount.HasValue)
            {
                TrailingAmount = request.TrailingAmount.Value;
            }
            if (request.LimitOffset.HasValue)
            {
                LimitOffset = request.LimitOffset.Value;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Messages.TrailingStopLimitOrder.ToString(this);
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new TrailingStopLimitOrder
            { 
                StopPrice = StopPrice,
                TrailingAmount = TrailingAmount,
                TrailingAsPercentage = TrailingAsPercentage,
                LimitPrice = LimitPrice, 
                LimitOffset = LimitOffset,
                StopTriggered = StopTriggered
            };
            CopyTo(order);
            return order;
        }

        /// <summary>
        /// Tries to update the stop price for a trailing stop order given the current market price
        /// </summary>
        /// <param name="currentMarketPrice">The current market price</param>
        /// <param name="currentStopPrice">The current trailing stop order stop price</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="limitOffset">The limit offset amount used to update the limit price</param>
        /// <param name="direction">The order direction</param>
        /// <param name="updatedStopPrice">The updated stop price</param>
        /// <param name="updatedLimitPrice">The updated limit price</param>
        /// <returns>
        /// Whether the stop price was updated.
        /// This only happens when the distance between the current stop price and the current market price is greater than the trailing amount,
        /// which will happen when the market price raises/falls for sell/buy orders respectively.
        /// </returns>
        public static bool TryUpdateStopAndLimitPrices(decimal currentMarketPrice, decimal currentStopPrice, decimal trailingAmount,
            bool trailingAsPercentage, decimal limitOffset, OrderDirection direction, out decimal updatedStopPrice, out decimal updatedLimitPrice)
        {
            updatedStopPrice = 0m;
            updatedLimitPrice = 0m;

            var distanceToMarketPrice = direction == OrderDirection.Sell
                ? currentMarketPrice - currentStopPrice
                : currentStopPrice - currentMarketPrice;
            var stopReference = trailingAsPercentage ? currentMarketPrice * trailingAmount : trailingAmount;

            if (distanceToMarketPrice <= stopReference)
            {
                return false;
            }

            updatedStopPrice = CalculateStopPrice(currentMarketPrice, trailingAmount, trailingAsPercentage, direction);
            updatedLimitPrice = CalculateLimitPrice(updatedStopPrice, limitOffset, direction);
            return true;
        }

        /// <summary>
        /// Calculates the stop price for a trailing stop limit order given the current market price
        /// </summary>
        /// <param name="currentMarketPrice">The current market price</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="direction">The order direction</param>
        /// <returns>The stop price for the order given the current market price</returns>
        public static decimal CalculateStopPrice(decimal currentMarketPrice, decimal trailingAmount, bool trailingAsPercentage,
            OrderDirection direction)
        {
            if (trailingAsPercentage)
            {
                return direction == OrderDirection.Buy
                    ? currentMarketPrice * (1 + trailingAmount)
                    : currentMarketPrice * (1 - trailingAmount);
            }

            return direction == OrderDirection.Buy
                ? currentMarketPrice + trailingAmount
                : currentMarketPrice - trailingAmount;
        }

        /// <summary>
        /// Calculates the limit price for a trailing stop limit order given the stop price and current market price
        /// </summary>
        /// <param name="currentStopPrice">The current stop price of the trailing stop limit order</param>
        /// <param name="limitOffset">The limit offset amount used to update the limit price</param>
        /// <param name="direction">The order direction</param>
        /// <returns>The stop price for the order given the current market price</returns>
        public static decimal CalculateLimitPrice(decimal currentStopPrice, decimal limitOffset, OrderDirection direction)
        {
            return direction == OrderDirection.Buy
                ? currentStopPrice + limitOffset
                : currentStopPrice - limitOffset;
        }
    }
}

