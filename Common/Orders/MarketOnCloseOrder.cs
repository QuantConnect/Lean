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
    /// Market on close order type - submits a market order on exchange close
    /// </summary>
    public class MarketOnCloseOrder : Order
    {
        /// <summary>
        /// Value of the order at limit price if a limit order, or market price if a market order.
        /// </summary>
        public override decimal Value
        {
            get { return AbsoluteQuantity * Price; }
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
                Type = OrderType.MarketOnClose
            };
        }

        /// <summary>
        /// Copy order before submitting to broker for update.
        /// </summary>
        public override Order Copy()
        {
            var target = new MarketOnCloseOrder();
            CopyTo(target);

            return target;
        }
        /// <summary>
        /// Intiializes a new instance of the <see cref="MarketOnCloseOrder"/> class.
        /// </summary>
        public MarketOnCloseOrder()
            : base(OrderType.MarketOnClose)
        {
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="MarketOnCloseOrder"/> class.
        /// </summary>
        /// <param name="symbol">The security's symbol being ordered</param>
        /// <param name="type">The security type of the symbol</param>
        /// <param name="quantity">The number of units to order</param>
        /// <param name="time">The current time</param>
        /// <param name="marketPrice">The current market price of the security, used to estimate the value of the order</param>
        /// <param name="tag">A user defined tag for the order</param>
        public MarketOnCloseOrder(string symbol, SecurityType type, int quantity, DateTime time, decimal marketPrice = 0m, string tag = "")
            : base(symbol, quantity, OrderType.MarketOnClose, time, marketPrice, tag, type)
        {
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="MarketOnCloseOrder"/> class.
        /// </summary>
        /// <param name="request">Submit order request.</param>
        public MarketOnCloseOrder(SubmitOrderRequest request)
            : base(request) { }
    }
}
