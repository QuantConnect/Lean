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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Order struct for placing new trade
    /// </summary>
    public abstract class Order 
    {
        /// <summary>
        /// Order ID.
        /// </summary>
        public int Id;

        /// <summary>
        /// Order id to process before processing this order.
        /// </summary>
        public int ContingentId;

        /// <summary>
        /// Brokerage Id for this order for when the brokerage splits orders into multiple pieces
        /// </summary>
        public List<long> BrokerId;

        /// <summary>
        /// Symbol of the Asset
        /// </summary>
        public string Symbol;
        
        /// <summary>
        /// Price of the Order.
        /// </summary>
        public decimal Price;

        /// <summary>
        /// Time the order was created.
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public int Quantity;

        /// <summary>
        /// Order Type
        /// </summary>
        public OrderType Type { get; private set; }

        /// <summary>
        /// Status of the Order
        /// </summary>
        public OrderStatus Status;

        /// <summary>
        /// Order duration - GTC or Day. Day not supported in backtests.
        /// </summary>
        public OrderDuration Duration = OrderDuration.GTC;

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag;

        /// <summary>
        /// The symbol's security type
        /// </summary>
        public SecurityType SecurityType = SecurityType.Equity;

        /// <summary>
        /// Order Direction Property based off Quantity.
        /// </summary>
        public OrderDirection Direction 
        {
            get 
            {
                if (Quantity > 0) 
                {
                    return OrderDirection.Buy;
                } 
                if (Quantity < 0) 
                {
                    return OrderDirection.Sell;
                }
                return OrderDirection.Hold;
            }
        }

        /// <summary>
        /// Get the absolute quantity for this order
        /// </summary>
        public decimal AbsoluteQuantity
        {
            get { return Math.Abs(Quantity); }
        }

        /// <summary>
        /// Value of the order at limit price if a limit order, or market price if a market order.
        /// </summary>
        public abstract decimal Value 
        { 
            get; 
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        protected Order(OrderType type)
        {
            Time = new DateTime();
            Price = 0;
            Type = type;
            Quantity = 0;
            Symbol = "";
            Status = OrderStatus.None;
            Tag = "";
            SecurityType = SecurityType.Base;
            Duration = OrderDuration.GTC;
            BrokerId = new List<long>();
            ContingentId = 0;
        }

        /// <summary>
        /// New order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type">Type of the security order</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="order">Order type (market, limit or stoploss order)</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="price">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        protected Order(string symbol, int quantity, OrderType order, DateTime time, decimal price = 0, string tag = "", SecurityType type = SecurityType.Base)
        {
            Time = time;
            Price = price;
            Type = order;
            Quantity = quantity;
            Symbol = symbol;
            Status = OrderStatus.None;
            Tag = tag;
            SecurityType = type;
            Duration = OrderDuration.GTC;
            BrokerId = new List<long>();
            ContingentId = 0;
        }

        /// <summary>
        /// New order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type"></param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="order">Order type (market, limit or stoploss order)</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="price">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        protected Order(string symbol, SecurityType type, int quantity, OrderType order, DateTime time, decimal price = 0, string tag = "") 
        {
            Time = time;
            Price = price;
            Type = order;
            Quantity = quantity;
            Symbol = symbol;
            Status = OrderStatus.None;
            Tag = tag;
            SecurityType = type;
            Duration = OrderDuration.GTC;
            BrokerId = new List<long>();
            ContingentId = 0;
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
            return string.Format("{0} order for {1} unit{3} of {2}", Type, Quantity, Symbol, Quantity == 1 ? "" : "s");
        }
    }
}
