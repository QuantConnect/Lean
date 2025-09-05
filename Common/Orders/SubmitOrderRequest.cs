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
    /// Defines a request to submit a new order
    /// </summary>
    public class SubmitOrderRequest : OrderRequest
    {
        /// <summary>
        /// Gets <see cref="Orders.OrderRequestType.Submit"/>
        /// </summary>
        public override OrderRequestType OrderRequestType
        {
            get { return OrderRequestType.Submit; }
        }

        /// <summary>
        /// Gets the security type of the symbol
        /// </summary>
        public SecurityType SecurityType
        {
            get; private set;
        }

        /// <summary>
        /// Gets the symbol to be traded
        /// </summary>
        public Symbol Symbol
        {
            get; private set;
        }

        /// <summary>
        /// Gets the order type od the order
        /// </summary>
        public OrderType OrderType
        {
            get; private set;
        }

        /// <summary>
        /// Gets the quantity of the order
        /// </summary>
        public decimal Quantity
        {
            get; private set;
        }

        /// <summary>
        /// Gets the limit price of the order, zero if not a limit order
        /// </summary>
        public decimal LimitPrice
        {
            get; private set;
        }

        /// <summary>
        /// Gets the stop price of the order, zero if not a stop order
        /// </summary>
        public decimal StopPrice
        {
            get; private set;
        }

        /// <summary>
        /// Price which must first be reached before a limit order can be submitted.
        /// </summary>
        public decimal TriggerPrice
        {
            get; private set;
        }

        /// <summary>
        /// Trailing amount for a trailing stop order
        /// </summary>
        public decimal TrailingAmount
        {
            get; private set;
        }

        /// <summary>
        /// Determines whether the <see cref="TrailingAmount"/> is a percentage or an absolute currency value
        /// </summary>
        public bool TrailingAsPercentage
        {
            get; private set;
        }

        /// <summary>
        /// Gets the order properties for this request
        /// </summary>
        public IOrderProperties OrderProperties
        {
            get; private set;
        }

        /// <summary>
        /// Gets the manager for the combo order. If null, the order is not a combo order.
        /// </summary>
        public GroupOrderManager GroupOrderManager
        {
            get; private set;
        }

        /// <summary>
        /// Whether this request should be asynchronous,
        /// which means the ticket will be returned to the algorithm without waiting for submission
        /// </summary>
        public bool Asynchronous
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitOrderRequest"/> class.
        /// The <see cref="OrderRequest.OrderId"/> will default to <see cref="OrderResponseErrorCode.UnableToFindOrder"/>
        /// </summary>
        /// <param name="orderType">The order type to be submitted</param>
        /// <param name="securityType">The symbol's <see cref="SecurityType"/></param>
        /// <param name="symbol">The symbol to be traded</param>
        /// <param name="quantity">The number of units to be ordered</param>
        /// <param name="stopPrice">The stop price for stop orders, non-stop orders this value is ignored</param>
        /// <param name="limitPrice">The limit price for limit orders, non-limit orders this value is ignored</param>
        /// <param name="triggerPrice">The trigger price for limit if touched orders, for non-limit if touched orders this value is ignored</param>
        /// <param name="trailingAmount">The trailing amount to be used to update the stop price</param>
        /// <param name="trailingAsPercentage">Whether the <paramref name="trailingAmount"/> is a percentage or an absolute currency value</param>
        /// <param name="time">The time this request was created</param>
        /// <param name="tag">A custom tag for this request</param>
        /// <param name="properties">The order properties for this request</param>
        /// <param name="groupOrderManager">The manager for this combo order</param>
        /// <param name="asynchronous">True if this request should be asynchronous,
        /// which means the ticket will be returned to the algorithm without waiting for submission</param>
        public SubmitOrderRequest(
            OrderType orderType,
            SecurityType securityType,
            Symbol symbol,
            decimal quantity,
            decimal stopPrice,
            decimal limitPrice,
            decimal triggerPrice,
            decimal trailingAmount,
            bool trailingAsPercentage,
            DateTime time,
            string tag,
            IOrderProperties properties = null,
            GroupOrderManager groupOrderManager = null,
            bool asynchronous = false
            )
            : base(time, (int)OrderResponseErrorCode.UnableToFindOrder, tag)
        {
            SecurityType = securityType;
            Symbol = symbol;
            GroupOrderManager = groupOrderManager;
            OrderType = orderType;
            Quantity = quantity;
            LimitPrice = limitPrice;
            StopPrice = stopPrice;
            TriggerPrice = triggerPrice;
            TrailingAmount = trailingAmount;
            TrailingAsPercentage = trailingAsPercentage;
            OrderProperties = properties;
            Asynchronous = asynchronous;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitOrderRequest"/> class.
        /// The <see cref="OrderRequest.OrderId"/> will default to <see cref="OrderResponseErrorCode.UnableToFindOrder"/>
        /// </summary>
        /// <param name="orderType">The order type to be submitted</param>
        /// <param name="securityType">The symbol's <see cref="SecurityType"/></param>
        /// <param name="symbol">The symbol to be traded</param>
        /// <param name="quantity">The number of units to be ordered</param>
        /// <param name="stopPrice">The stop price for stop orders, non-stop orders this value is ignored</param>
        /// <param name="limitPrice">The limit price for limit orders, non-limit orders this value is ignored</param>
        /// <param name="triggerPrice">The trigger price for limit if touched orders, for non-limit if touched orders this value is ignored</param>
        /// <param name="time">The time this request was created</param>
        /// <param name="tag">A custom tag for this request</param>
        /// <param name="properties">The order properties for this request</param>
        /// <param name="groupOrderManager">The manager for this combo order</param>
        /// <param name="asynchronous">True if this request should be asynchronous,
        /// which means the ticket will be returned to the algorithm without waiting for submission</param>
        public SubmitOrderRequest(
            OrderType orderType,
            SecurityType securityType,
            Symbol symbol,
            decimal quantity,
            decimal stopPrice,
            decimal limitPrice,
            decimal triggerPrice,
            DateTime time,
            string tag,
            IOrderProperties properties = null,
            GroupOrderManager groupOrderManager = null,
            bool asynchronous = false
            )
            : this(orderType, securityType, symbol, quantity, stopPrice, limitPrice, triggerPrice, 0, false, time, tag, properties,
                  groupOrderManager, asynchronous)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitOrderRequest"/> class.
        /// The <see cref="OrderRequest.OrderId"/> will default to <see cref="OrderResponseErrorCode.UnableToFindOrder"/>
        /// </summary>
        /// <param name="orderType">The order type to be submitted</param>
        /// <param name="securityType">The symbol's <see cref="SecurityType"/></param>
        /// <param name="symbol">The symbol to be traded</param>
        /// <param name="quantity">The number of units to be ordered</param>
        /// <param name="stopPrice">The stop price for stop orders, non-stop orders this value is ignored</param>
        /// <param name="limitPrice">The limit price for limit orders, non-limit orders this value is ignored</param>
        /// <param name="time">The time this request was created</param>
        /// <param name="tag">A custom tag for this request</param>
        /// <param name="properties">The order properties for this request</param>
        /// <param name="groupOrderManager">The manager for this combo order</param>
        /// <param name="asynchronous">True if this request should be asynchronous,
        /// which means the ticket will be returned to the algorithm without waiting for submission</param>
        public SubmitOrderRequest(
            OrderType orderType,
            SecurityType securityType,
            Symbol symbol,
            decimal quantity,
            decimal stopPrice,
            decimal limitPrice,
            DateTime time,
            string tag,
            IOrderProperties properties = null,
            GroupOrderManager groupOrderManager = null,
            bool asynchronous = false
            )
            : this(orderType, securityType, symbol, quantity, stopPrice, limitPrice, 0, time, tag, properties, groupOrderManager, asynchronous)
        {
        }

        /// <summary>
        /// Sets the <see cref="OrderRequest.OrderId"/>
        /// </summary>
        /// <param name="orderId">The order id of the generated order</param>
        internal void SetOrderId(int orderId)
        {
            OrderId = orderId;
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
            return Messages.SubmitOrderRequest.ToString(this);
        }
    }
}
