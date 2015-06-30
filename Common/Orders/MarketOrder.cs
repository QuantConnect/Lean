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
    /// Market order type definition
    /// </summary>
    public class MarketOrder : Order
    {
        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        public MarketOrder()
            : base(OrderType.Market)
        {
        }

        /// <summary>
        /// Value of the order at market price.
        /// </summary>
        public override decimal Value
        {
            get
            {
                return Convert.ToDecimal(Quantity) * Price;
            }
        }

        /// <summary>
        /// Create update request for pending orders. Null values will be ignored.
        /// </summary>
        public UpdateOrderRequest UpdateRequest(int? quantity = null, string tag = null)
        {
            return new UpdateOrderRequest
            {
                Id = Guid.NewGuid(),
                OrderId = Id,
                Created = DateTime.Now,
                Quantity = quantity ?? Quantity,
                Tag = tag ?? Tag
            };
        }

        /// <summary>
        /// Apply changes after the update request is processed.
        /// </summary>
        public override void ApplyUpdate(UpdateOrderRequest request)
        {
            Quantity = request.Quantity;
            Tag = request.Tag;
        }

        /// <summary>
        /// Create submit request.
        /// </summary>
        public static SubmitOrderRequest SubmitRequest(string symbol, int quantity, string tag, SecurityType securityType, decimal price = 0, DateTime? time = null)
        {
            return new SubmitOrderRequest
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Quantity = quantity,
                Tag = tag,
                SecurityType = securityType,
                Created = time ?? DateTime.Now,
                Price = price,
                Type = OrderType.Market
            };
        }

        /// <summary>
        /// Copy order before submitting to broker for update.
        /// </summary>
        public override Order Copy()
        {
            var target = new MarketOrder();
            CopyTo(target);

            return target;
        }

        /// <summary>
        /// New market order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type">Type of the security order</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="tag">User defined data tag for this order</param>
        public MarketOrder(string symbol, int quantity, DateTime time, string tag = "", SecurityType type = SecurityType.Base, decimal price = 0m) :
            base(symbol, quantity, OrderType.Market, time, 0, tag, type)
        {
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="MarketOrder"/> class.
        /// </summary>
        /// <param name="request">Submit order request.</param>
        public MarketOrder(SubmitOrderRequest request)
            : this(request.Symbol, request.Quantity, request.Created, request.Tag, request.SecurityType, request.Price) { }
    }

} // End QC Namespace:
