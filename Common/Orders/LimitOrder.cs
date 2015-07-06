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
        public decimal LimitPrice;

        /// <summary>
        /// Value of the order at limit price if a limit order
        /// </summary>
        public override decimal Value
        {
            get { return Quantity*LimitPrice; }
        }

        /// <summary>
        /// Create update request for pending orders. Null values will be ignored.
        /// </summary>
        public UpdateOrderRequest CreateUpdateRequest(int? quantity = null, decimal? limitPrice = null, string tag = null)
        {
            return new UpdateOrderRequest
            {
                Id = Guid.NewGuid(),
                OrderId = Id,
                Created = DateTime.Now,
                Quantity = quantity ?? Quantity,
                LimitPrice = limitPrice ?? LimitPrice,
                Tag = tag ?? Tag
            };
        }

        /// <summary>
        /// Apply changes after the update request is processed.
        /// </summary>
        public override void ApplyUpdate(UpdateOrderRequest request)
        {
            base.ApplyUpdate(request);

            LimitPrice = request.LimitPrice;
        }

        /// <summary>
        /// Create submit request.
        /// </summary>
        public static SubmitOrderRequest CreateSubmitRequest(SecurityType securityType, string symbol, int quantity, DateTime time, decimal limitPrice, string tag = null)
        {
            return new SubmitOrderRequest
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Quantity = quantity,
                Tag = tag,
                SecurityType = securityType,
                Created = time,
                LimitPrice = limitPrice,
                Type = OrderType.Limit
            };
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        public LimitOrder()
            : base (OrderType.Limit)
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
        public LimitOrder(string symbol, int quantity, decimal limitPrice, DateTime time, string tag = "", SecurityType type = SecurityType.Base) :
            base(symbol, quantity, OrderType.Limit, time, 0, tag, type)
        {
            LimitPrice = limitPrice;

            if (tag == "")
            {
                //Default tag values to display limit price in GUI.
                Tag = "Limit Price: " + limitPrice.ToString("C");
            }
        }

        /// <summary>
        /// New limit order constructor
        /// </summary>
        /// <param name="request">Submit order request.</param>
        public LimitOrder(SubmitOrderRequest request) :
            base(request)
        {
            LimitPrice = request.LimitPrice;

            if (Tag == "")
            {
                //Default tag values to display limit price in GUI.
                Tag = "Limit Price: " + LimitPrice.ToString("C");
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
            return string.Format("{0} order for {1} unit{2} of {3} at limit {4}", Type, Quantity, Quantity == 1 ? "" : "s", Symbol, LimitPrice);
        }

        /// <summary>
        /// Copy order before submitting to broker for update.
        /// </summary>
        public override Order Clone()
        {
            var target = new LimitOrder();
            CopyTo(target);
            target.LimitPrice = LimitPrice;

            return target;
        }
    }
}
