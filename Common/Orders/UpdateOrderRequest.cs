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
using System.Collections.Generic;
using static QuantConnect.StringExtensions;

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
            var updates = new List<string>();
            if (Quantity.HasValue)
            {
                updates.Add(Invariant($"Quantity: {Quantity.Value}"));
            }
            if (LimitPrice.HasValue)
            {
                updates.Add(Invariant($"LimitPrice: {LimitPrice.Value.SmartRounding()}"));
            }
            if (StopPrice.HasValue)
            {
                updates.Add(Invariant($"StopPrice: {StopPrice.Value.SmartRounding()}"));
            }

            return Invariant($"{Time} UTC: Update Order: ({OrderId}) - {string.Join(", ", updates)} {Tag} Status: {Status}");
        }
    }
}