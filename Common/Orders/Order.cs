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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /******************************************************** 
    * ORDER CLASS DEFINITION
    *********************************************************/
    /// <summary>
    /// Type of the order: market, limit or stop
    /// </summary>
    public enum OrderType 
    {
        /// <summary>
        /// Market Order Type
        /// </summary>
        Market,

        /// <summary>
        /// Limit Order Type
        /// </summary>
        Limit,

        /// <summary>
        /// Stop Market Order Type - Fill at market price when break target price
        /// </summary>
        StopMarket
    }


    /// <summary>
    /// Order duration in market
    /// </summary>
    public enum OrderDuration
    { 
        /// <summary>
        /// Order good until its filled.
        /// </summary>
        GTC,

        /// <summary>
        /// Order valid for today only: -- CURRENTLY ONLY GTC ORDER DURATION TYPE IN BACKTESTS.
        /// </summary>
        //Day
    }


    /// <summary>
    /// Direction of the order
    /// </summary>
    public enum OrderDirection {

        /// <summary>
        /// Buy Order 
        /// </summary>
        Buy,

        /// <summary>
        /// Sell Order
        /// </summary>
        Sell,

        /// <summary>
        /// Default Value - No Order Direction
        /// </summary>
        Hold
    }


    /// <summary>
    /// Fill status of the order class.
    /// </summary>
    public enum OrderStatus {
        
        /// <summary>
        /// New order pre-submission to the order processor.
        /// </summary>
        New,

        /// <summary>
        /// Order flagged for updating the inmarket order.
        /// </summary>
        Update,

        /// <summary>
        /// Order submitted to the market
        /// </summary>
        Submitted,

        /// <summary>
        /// Partially filled, In Market Order.
        /// </summary>
        PartiallyFilled,

        /// <summary>
        /// Completed, Filled, In Market Order.
        /// </summary>
        Filled,

        /// <summary>
        /// Order cancelled before it was filled
        /// </summary>
        Canceled,

        /// <summary>
        /// No Order State Yet
        /// </summary>
        None,

        /// <summary>
        /// Order invalidated before it hit the market (e.g. insufficient capital)..
        /// </summary>
        Invalid
    }

    /// <summary>
    /// Indexed order error codes:
    /// </summary>
    public static class OrderErrors 
    {
        /// <summary>
        /// Order validation error codes
        /// </summary>
        public static Dictionary<int, string> ErrorTypes = new Dictionary<int, string>() 
        {
            {-1, "Order quantity must not be zero"},
            {-2, "There is no data yet for this security - please wait for data (market order price not available yet)"},
            {-3, "Attempting market order outside of market hours"},
            {-4, "Insufficient capital to execute order"},
            {-5, "Exceeded maximum allowed orders for one analysis period"},
            {-6, "Order timestamp error. Order appears to be executing in the future"},
            {-7, "General error in order"},
            {-8, "Order has already been filled and cannot be modified"},
        };
    }


    /// <summary>
    /// Order struct for placing new trade
    /// </summary>
    public class Order 
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
        /// Brokerage Id for this order.
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
        public OrderType Type;

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
        public string Tag = "";

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
                else if (Quantity < 0) 
                {
                    return OrderDirection.Sell;
                } 
                else 
                {
                    return OrderDirection.Hold;
                }
            }
        }

        /// <summary>
        /// Get the absolute quantity for this order
        /// </summary>
        public decimal AbsoluteQuantity 
        {
            get 
            {
                return Math.Abs(Quantity);
            }
        }

        /// <summary>
        /// Value of the order at limit price if a limit order, or market price if a market order.
        /// </summary>
        public decimal Value 
        {
            get
            {
                return Convert.ToDecimal(Quantity) * Price;
            }
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
        public Order(string symbol, int quantity, OrderType order, DateTime time, decimal price = 0, string tag = "", SecurityType type = SecurityType.Base)
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
        public Order(string symbol, SecurityType type, int quantity, OrderType order, DateTime time, decimal price = 0, string tag = "") 
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
    }

} // End QC Namespace:
