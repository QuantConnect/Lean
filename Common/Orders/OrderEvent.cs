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
    /// Order Event - Messaging class signifying a change in an order state and record the change in the user's algorithm portfolio 
    /// </summary>
    public class OrderEvent
    {
        /// <summary>
        /// Id of the order this event comes from.
        /// </summary>
        public int OrderId;

        /// <summary>
        /// Easy access to the order symbol associated with this event.
        /// </summary>
        public string Symbol;

        /// <summary>
        /// Status message of the order.
        /// </summary>
        public OrderStatus Status;

        /// <summary>
        /// Fill price information about the order
        /// </summary>
        public decimal FillPrice;

        /// <summary>
        /// Number of shares of the order that was filled in this event.
        /// </summary>
        public int FillQuantity;

        /// <summary>
        /// Public Property Absolute Getter of Quantity -Filled
        /// </summary>
        public int AbsoluteFillQuantity 
        {
            get 
            {
                return Math.Abs(FillQuantity);
            }
        }

        /// <summary>
        /// Order direction.
        /// </summary>
        public OrderDirection Direction
        {
            get; private set;
        }

        /// <summary>
        /// Any message from the exchange.
        /// </summary>
        public string Message;

        /// <summary>
        /// Order Constructor.
        /// </summary>
        /// <param name="id">Id of the parent order</param>
        /// <param name="symbol">Asset Symbol</param>
        /// <param name="status">Status of the order</param>
        /// <param name="direction">The direction of the order this event belongs to</param>
        /// <param name="fillPrice">Fill price information if applicable.</param>
        /// <param name="fillQuantity">Fill quantity</param>
        /// <param name="message">Message from the exchange</param>
        public OrderEvent(int id, string symbol, OrderStatus status, OrderDirection direction, decimal fillPrice, int fillQuantity, string message = "")
        {
            OrderId = id;
            Status = status;
            FillPrice = fillPrice;
            Message = message;
            FillQuantity = fillQuantity;
            Symbol = symbol;
            Direction = direction;
        }

        /// <summary>
        /// Helper Constructor using Order to Initialize.
        /// </summary>
        /// <param name="order">Order for this order status</param>
        /// <param name="message">Message from exchange or QC.</param>
        public OrderEvent(Order order, string message = "") 
        {
            OrderId = order.Id;
            Status = order.Status;
            Message = message;
            Symbol = order.Symbol;
            Direction = order.Direction;

            //Initialize to zero, manually set fill quantity
            FillQuantity = 0;
            FillPrice = 0;
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
            return FillQuantity == 0 
                ? string.Format("OrderID: {0} Symbol: {1} Status: {2}", OrderId, Symbol, Status) 
                : string.Format("OrderID: {0} Symbol: {1} Status: {2} Quantity: {3} FillPrice: {4}", OrderId, Symbol, Status, FillQuantity, FillPrice);
        }
    }

} // End QC Namespace:
