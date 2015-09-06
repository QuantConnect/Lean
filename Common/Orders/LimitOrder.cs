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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Limit order type definition
    /// </summary>
    public class LimitOrder : Order
    {
        /// <summary>
        /// Limit price for this order.
        /// </summary>
        public decimal LimitPrice { get; internal set; }

        /// <summary>
        /// Limit Order Type
        /// </summary>
        public override OrderType Type
        {
            get { return OrderType.Limit; }
        }

        /// <summary>
        /// Value of the order at limit price if a limit order
        /// </summary>
        public override decimal Value
        {
            get { return Quantity*LimitPrice; }
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        public LimitOrder()
        {
        }

        /// <summary>
        /// New limit order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type">Type of the security order</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="limitPrice">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        public LimitOrder(Symbol symbol, int quantity, decimal limitPrice, DateTime time, string tag = "", SecurityType type = SecurityType.Base)
            : base(symbol, quantity, time, tag, type)
        {
            LimitPrice = limitPrice;

            if (tag == "")
            {
                //Default tag values to display limit price in GUI.
                Tag = "Limit Price: " + limitPrice.ToString("C");
            }
        }

        /// <summary>
        /// Gets the value of this order at the given market price.
        /// </summary>
        /// <param name="currentMarketPrice">The current market price of the security</param>
        /// <returns>The value of this order given the current market price</returns>
        public override decimal GetValue(decimal currentMarketPrice)
        {
            return Quantity*LimitPrice;
        }

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public override void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            base.ApplyUpdateOrderRequest(request);
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
            return string.Format("{0} at limit {1}", base.ToString(), LimitPrice.SmartRounding());
        }

        /// <summary>
        /// Creates a deep-copy clone of this order
        /// </summary>
        /// <returns>A copy of this order</returns>
        public override Order Clone()
        {
            var order = new LimitOrder {LimitPrice = LimitPrice};
            CopyTo(order);
            return order;
        }
    }
}
