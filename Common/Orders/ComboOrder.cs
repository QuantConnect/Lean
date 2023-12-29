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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Combo order type
    /// </summary>
    public abstract class ComboOrder : Order
    {
        private decimal _ratio;

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public override decimal Quantity
        {
            get
            {
                return _ratio.GetOrderLegGroupQuantity(GroupOrderManager).Normalize();
            }
            internal set
            {
                _ratio = value.GetOrderLegRatio(GroupOrderManager);
            }
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        public ComboOrder() : base()
        {
        }

        /// <summary>
        /// New market order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="groupOrderManager">Manager for the orders in the group</param>
        /// <param name="tag">User defined data tag for this order</param>
        /// <param name="properties">The order properties for this order</param>
        public ComboOrder(Symbol symbol, decimal quantity, DateTime time, GroupOrderManager groupOrderManager, string tag = "",
            IOrderProperties properties = null)
            : base(symbol, 0m, time, tag, properties)
        {
            GroupOrderManager = groupOrderManager;
            Quantity = quantity;
        }

        /// <summary>
        /// Modifies the state of this order to match the update request
        /// </summary>
        /// <param name="request">The request to update this order object</param>
        public override void ApplyUpdateOrderRequest(UpdateOrderRequest request)
        {
            if (request.OrderId != Id)
            {
                throw new ArgumentException("Attempted to apply updates to the incorrect order!");
            }
            if (request.Tag != null)
            {
                Tag = request.Tag;
            }
            if (request.Quantity.HasValue)
            {
                this.UpdateQuantity(request.Quantity.Value);
            }
        }
    }
}
