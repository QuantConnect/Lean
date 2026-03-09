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
using Newtonsoft.Json;
using QuantConnect.Interfaces;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Trailing Stop Order Type Definition
    /// </summary>
    public class TrailingStopOrder : StopMarketOrder
    {
        /// <summary>
        /// Trailing amount for this trailing stop order
        /// </summary>
        [JsonProperty(PropertyName = "trailingAmount")]
        public decimal TrailingAmount { get; internal set; }

        /// <summary>
        /// Determines whether the <see cref="TrailingAmount"/> is a percentage or an absolute currency value
        /// </summary>
        [JsonProperty(PropertyName = "trailingAsPercentage")]
        public bool TrailingAsPercentage { get; internal set; }

        /// <summary>
        /// StopLimit Order Type
        /// </summary>
        public override OrderType Type
        {
            get { return OrderType.TrailingStop; }
        }

        /// <summary>
        /// Default constructor for JSON Deserialization:
        /// </summary>
        public TrailingStopOrder()
        {
        }

        /// <summary>
        /// New Trailing Stop Market Order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset being traded</param>
        /// <param name="quantity">Quantity of the asset to be traded</param>
        /// <param name="stopPrice">Initial stop price at which the order should be triggered</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The properties for this order</param>
        public TrailingStopOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal trailingAmount, bool trailingAsPercentage,
            DateTime time, string tag = "", IOrderProperties properties = null)
            : base(symbol, quantity, stopPrice, time, tag, properties)
        {
            TrailingAmount = trailingAmount;
            TrailingAsPercentage = trailingAsPercentage;
        }

        /// <summary>
        /// New Trailing Stop Market Order constructor.
        /// It creates a new Trailing Stop Market Order with an initial stop price calculated by subtracting (for a sell) or adding (for a buy) the
        /// trailing amount to the current market price.
        /// </summary>
        /// <param name="symbol">Symbol asset being traded</param>
        /// <param name="quantity">Quantity of the asset to be traded</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The properties for this order</param>
        public TrailingStopOrder(Symbol symbol, decimal quantity, decimal trailingAmount, bool trailingAsPercentage,
            DateTime time, string tag = "", IOrderProperties properties = null)
            : this(symbol, quantity, 0, trailingAmount, trailingAsPercentage, time, tag, properties)
        {
        }

        /// <summary>
        /// Gets the default tag for this order
        /// </summary>
        /// <returns>The default tag</returns>
        public override string GetDefaultTag()
        {
            return Messages.TrailingStopOrder.Tag(this);
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
            return Messages.TrailingStopOrder.ToString(this);
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new TrailingStopOrder
            {
                StopPrice = StopPrice,
                TrailingAmount = TrailingAmount,
                TrailingAsPercentage = TrailingAsPercentage
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
        /// <param name="direction">The order direction</param>
        /// <param name="updatedStopPrice">The updated stop price</param>
        /// <returns>
        /// Whether the stop price was updated.
        /// This only happens when the distance between the current stop price and the current market price is greater than the trailing amount,
        /// which will happen when the market price raises/falls for sell/buy orders respectively.
        /// </returns>
        public static bool TryUpdateStopPrice(decimal currentMarketPrice, decimal currentStopPrice, decimal trailingAmount,
            bool trailingAsPercentage, OrderDirection direction, out decimal updatedStopPrice)
        {
            updatedStopPrice = 0m;
            var distanceToMarketPrice = direction == OrderDirection.Sell
                ? currentMarketPrice - currentStopPrice
                : currentStopPrice - currentMarketPrice;
            var stopReference = trailingAsPercentage ? currentMarketPrice * trailingAmount : trailingAmount;

            if (distanceToMarketPrice <= stopReference)
            {
                return false;
            }

            updatedStopPrice = CalculateStopPrice(currentMarketPrice, trailingAmount, trailingAsPercentage, direction);
            return true;
        }

        /// <summary>
        /// Calculates the stop price for a trailing stop order given the current market price
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
    }
}
