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
    /// Defines a request to update an order's values
    /// </summary>
    public class UpdateOrderRequest : OrderRequest
    {
        /// <summary>
        /// Gets <see cref="Orders.OrderRequestType.Update"/>
        /// </summary>
        public override OrderRequestType OrderRequestType
        {
            get { return OrderRequestType.Update; }
        }

        /// <summary>
        /// Gets the new quantity of the order, null to not change the quantity
        /// </summary>
        public decimal? Quantity { get; private set; }

        /// <summary>
        /// Gets the new limit price of the order, null to not change the limit price
        /// </summary>
        public decimal? LimitPrice { get; private set; }

        /// <summary>
        /// Gets the new stop price of the order, null to not change the stop price
        /// </summary>
        public decimal? StopPrice { get; private set; }

        /// <summary>
        /// Gets the new trigger price of the order, null to not change the trigger price
        /// </summary>
        public decimal? TriggerPrice { get; private set; }

        /// <summary>
        /// The trailing stop order trailing amount
        /// </summary>
        public decimal? TrailingAmount { get; private set; }

        /// <summary>
        /// The trailing stop limit order limit offset
        /// </summary>
        public decimal? LimitOffset { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOrderRequest"/> class
        /// </summary>
        /// <param name="time">The time the request was submitted</param>
        /// <param name="orderId">The order id to be updated</param>
        /// <param name="fields">The fields defining what should be updated</param>
        public UpdateOrderRequest(DateTime time, int orderId, UpdateOrderFields fields)
            : base(time, orderId, fields.Tag)
        {
            Quantity = fields.Quantity;
            LimitPrice = fields.LimitPrice;
            StopPrice = fields.StopPrice;
            TriggerPrice = fields.TriggerPrice;
            TrailingAmount = fields.TrailingAmount;
            LimitOffset = fields.LimitOffset;
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
            return Messages.UpdateOrderRequest.ToString(this);
        }

        /// <summary>
        /// Checks whether the update request is allowed for a closed order.
        /// Only tag updates are allowed on closed orders.
        /// </summary>
        /// <returns>True if the update request is allowed for a closed order</returns>
        public bool IsAllowedForClosedOrder()
        {
            return !Quantity.HasValue && !LimitPrice.HasValue && !StopPrice.HasValue && 
                !TriggerPrice.HasValue && !TrailingAmount.HasValue && !LimitOffset.HasValue;
        }
    }
}
