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
using QuantConnect.Orders.Fees;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order Event - Messaging class signifying a change in an order state and record the change in the user's algorithm portfolio
    /// </summary>
    public class OrderEvent
    {
        private decimal fillPrice;
        private decimal fillQuantity;

        /// <summary>
        /// Id of the order this event comes from.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Easy access to the order symbol associated with this event.
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// The date and time of this event (UTC).
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// Status message of the order.
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The fee associated with the order
        /// </summary>
        public OrderFee OrderFee { get; set; }

        /// <summary>
        /// Fill price information about the order
        /// </summary>
        public decimal FillPrice
        {
            get { return fillPrice; }
            set { fillPrice = value.Normalize(); }
        }

        /// <summary>
        /// Currency for the fill price
        /// </summary>
        public string FillPriceCurrency { get; set; }

        /// <summary>
        /// Number of shares of the order that was filled in this event.
        /// </summary>
        public decimal FillQuantity
        {
            get { return fillQuantity; }
            set { fillQuantity = value.Normalize(); }
        }

        /// <summary>
        /// Public Property Absolute Getter of Quantity -Filled
        /// </summary>
        public decimal AbsoluteFillQuantity => Math.Abs(FillQuantity);

        /// <summary>
        /// Order direction.
        /// </summary>
        public OrderDirection Direction { get; set; }

        /// <summary>
        /// Any message from the exchange.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// True if the order event is an assignment
        /// </summary>
        public bool IsAssignment { get; set; }

        /// <summary>
        /// Order Event Constructor.
        /// </summary>
        /// <param name="orderId">Id of the parent order</param>
        /// <param name="symbol">Asset Symbol</param>
        /// <param name="utcTime">Date/time of this event</param>
        /// <param name="status">Status of the order</param>
        /// <param name="direction">The direction of the order this event belongs to</param>
        /// <param name="fillPrice">Fill price information if applicable.</param>
        /// <param name="fillQuantity">Fill quantity</param>
        /// <param name="orderFee">The order fee</param>
        /// <param name="message">Message from the exchange</param>
        public OrderEvent(int orderId,
            Symbol symbol,
            DateTime utcTime,
            OrderStatus status,
            OrderDirection direction,
            decimal fillPrice,
            decimal fillQuantity,
            OrderFee orderFee,
            string message = ""
            )
        {
            OrderId = orderId;
            Symbol = symbol;
            UtcTime = utcTime;
            Status = status;
            Direction = direction;
            FillPrice = fillPrice;
            FillPriceCurrency = string.Empty;
            FillQuantity = fillQuantity;
            OrderFee = orderFee;
            Message = message;
            IsAssignment = false;
        }

        /// <summary>
        /// Helper Constructor using Order to Initialize.
        /// </summary>
        /// <param name="order">Order for this order status</param>
        /// <param name="utcTime">Date/time of this event</param>
        /// <param name="orderFee">The order fee</param>
        /// <param name="message">Message from exchange or QC.</param>
        public OrderEvent(Order order, DateTime utcTime, OrderFee orderFee, string message = "")
        {
            OrderId = order.Id;
            Symbol = order.Symbol;
            Status = order.Status;
            Direction = order.Direction;

            //Initialize to zero, manually set fill quantity
            FillQuantity = 0;
            FillPrice = 0;
            FillPriceCurrency = order.PriceCurrency;

            UtcTime = utcTime;
            OrderFee = orderFee;
            Message = message;
            IsAssignment = false;
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
            var message = FillQuantity == 0
                ? Invariant($"Time: {UtcTime} OrderID: {OrderId} Symbol: {Symbol.Value} Status: {Status}")
                : Invariant($"Time: {UtcTime} OrderID: {OrderId} Symbol: {Symbol.Value} Status: {Status} ")+
                  Invariant($"Quantity: {FillQuantity} FillPrice: {FillPrice.SmartRounding()} {FillPriceCurrency}");

            // attach the order fee so it ends up in logs properly.
            if (OrderFee.Value.Amount != 0m) message += $" OrderFee: {OrderFee}";

            // add message from brokerage
            if (!string.IsNullOrEmpty(Message))
            {
                message += $" Message: {Message}";
            }

            return message;
        }

        /// <summary>
        /// Returns a clone of the current object.
        /// </summary>
        /// <returns>The new clone object</returns>
        public OrderEvent Clone()
        {
            return (OrderEvent) MemberwiseClone();
        }
    }
}
