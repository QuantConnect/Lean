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
    /// Stop Market Order Type Definition
    /// </summary>
    public class StopMarketOrder : Order
    {
        /// <summary>
        /// Stop price for this stop market order.
        /// </summary>
        public decimal StopPrice;

        /// <summary>
        /// Value of the order at stop price
        /// </summary>
        public override decimal Value
        {
            get { return Quantity*StopPrice; }
        }

        /// <summary>
        /// Create update request for pending orders. Null values will be ignored.
        /// </summary>
        public UpdateOrderRequest UpdateRequest(int? quantity = null, decimal? stopPrice = null, string tag = null)
        {
            return new UpdateOrderRequest
            {
                Id = Guid.NewGuid(),
                OrderId = Id,
                Created = DateTime.Now,
                Quantity = quantity ?? Quantity,
                StopPrice = stopPrice ?? StopPrice,
                Tag = tag ?? Tag
            };
        }

        /// <summary>
        /// Apply changes after the update request is processed.
        /// </summary>
        public void ApplyUpdate(UpdateOrderRequest request)
        {
            Quantity = request.Quantity;
            StopPrice = request.StopPrice;
            Tag = request.Tag;
        }

        /// <summary>
        /// Create submit request.
        /// </summary>
        public static SubmitOrderRequest SubmitRequest(string symbol, int quantity, decimal stopPrice, string tag, SecurityType securityType, decimal price = 0, DateTime? time = null)
        {
            return new SubmitOrderRequest
            {
                Id = Guid.NewGuid(),
                Symbol = symbol,
                Quantity = quantity,
                Tag = tag,
                SecurityType = securityType,
                Created = time ?? DateTime.Now,
                StopPrice = stopPrice,
                Type = OrderType.StopMarket
            };
        }

        /// <summary>
        /// Copy order before submitting to broker for update.
        /// </summary>
        public override Order Copy()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<LimitOrder>(Newtonsoft.Json.JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Default constructor for JSON Deserialization:
        /// </summary>
        public StopMarketOrder()
            : base(OrderType.StopMarket)
        {
        }

        /// <summary>
        /// New Stop Market Order constructor - 
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type">Type of the security order</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="stopPrice">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        public StopMarketOrder(string symbol, int quantity, decimal stopPrice, DateTime time, string tag = "", SecurityType type = SecurityType.Base) :
            base(symbol, quantity, OrderType.StopMarket, time, stopPrice, tag, type)
        {
            StopPrice = stopPrice;

            if (tag == "")
            {
                //Default tag values to display stop price in GUI.
                Tag = "Stop Price: " + stopPrice.ToString("C");
            }
        }

        /// <summary>
        /// Intiializes a new instance of the <see cref="StopMarketOrder"/> class.
        /// </summary>
        /// <param name="request">Submit order request.</param>
        public StopMarketOrder(SubmitOrderRequest request) :
            this(request.Symbol, request.Quantity, request.StopPrice, request.Created, request.Tag, request.SecurityType) { }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("{0} order for {1} unit{2} of {3} at stop {4}", Type, Quantity, Quantity == 1 ? "" : "s", Symbol, StopPrice);
        }
    }
}
