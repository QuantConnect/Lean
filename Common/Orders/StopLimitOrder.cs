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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Stop Market Order Type Definition
    /// </summary>
    public class StopLimitOrder : Order
    {
        /// <summary>
        /// Stop price for this stop market order.
        /// </summary>
        public decimal StopPrice { get; internal set; }

        /// <summary>
        /// Signal showing the "StopLimitOrder" has been converted into a Limit Order
        /// </summary>
        public bool StopTriggered { get; internal set; }

        /// <summary>
        /// Limit price for the stop limit order
        /// </summary>
        public decimal LimitPrice { get; internal set; }

        /// <summary>
        /// StopLimit Order Type
        /// </summary>
        public override OrderType Type
        {
            get { return OrderType.StopLimit; }
        }

        /// <summary>
        /// Default constructor for JSON Deserialization:
        /// </summary>
        public StopLimitOrder()
        {
        }

        /// <summary>
        /// New Stop Market Order constructor -
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="limitPrice">Maximum price to fill the order</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="stopPrice">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        public StopLimitOrder(Symbol symbol, decimal quantity, decimal stopPrice, decimal limitPrice, DateTime time, string tag = "", IOrderProperties properties = null)
            : base(symbol, quantity, time, tag, properties)
        {
            StopPrice = stopPrice;
            LimitPrice = limitPrice;

            if (tag == "")
            {
                //Default tag values to display stop price in GUI.
                Tag = Invariant($"Stop Price: {stopPrice:C} Limit Price: {limitPrice:C}");
            }
        }

        /// <summary>
        /// Gets the order value in units of the security's quote currency
        /// </summary>
        /// <param name="security">The security matching this order's symbol</param>
        protected override decimal GetValueImpl(Security security)
        {
            // selling, so higher price will be used
            if (Quantity < 0)
            {
                return Quantity*Math.Max(LimitPrice, security.Price);
            }

            // buying, so lower price will be used
            if (Quantity > 0)
            {
                return Quantity*Math.Min(LimitPrice, security.Price);
            }

            return 0m;
        }

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public override void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            base.ApplyUpdateOrderRequest(request);
            if (request.StopPrice.HasValue)
            {
                StopPrice = request.StopPrice.Value;
            }
            if (request.LimitPrice.HasValue)
            {
                LimitPrice = request.LimitPrice.Value;
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
            return Invariant($"{base.ToString()} at stop {StopPrice.SmartRounding()} limit {LimitPrice.SmartRounding()}");
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new StopLimitOrder {StopPrice = StopPrice, LimitPrice = LimitPrice};
            CopyTo(order);
            return order;
        }
    }
}