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
using QuantConnect.Orders;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Securities.Interfaces
{
    /// <summary>
    /// Security transaction model interface for QuantConnect security objects
    /// </summary>
    /// <seealso cref="EquityTransactionModel"/>
    /// <seealso cref="ForexTransactionModel"/>
    public interface ISecurityTransactionModel
    {
        /// <summary>
        /// Model the slippage on a market order: fixed percentage of order price
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Order to update</param>
        OrderEvent MarketFill(Security asset, MarketOrder order);


        /// <summary>
        /// Stop Market Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Stop Order to Check, return filled if true</param>
        OrderEvent StopMarketFill(Security asset, StopMarketOrder order);

        /// <summary>
        /// Stop Limit Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        OrderEvent StopLimitFill(Security asset, StopLimitOrder order);

        /// <summary>
        /// Limit Fill Model. Return an order event with the fill details.
        /// </summary>
        /// <param name="asset">Stock Object to use to help model limit fill</param>
        /// <param name="order">Order to fill. Alter the values directly if filled.</param>
        OrderEvent LimitFill(Security asset, LimitOrder order);

        /// <summary>
        /// Market on Open Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        OrderEvent MarketOnOpenFill(Security asset, MarketOnOpenOrder order);

        /// <summary>
        /// Market on Close Fill Model. Return an order event with the fill details
        /// </summary>
        /// <param name="asset">Asset we're trading with this order</param>
        /// <param name="order">Order to be filled</param>
        /// <returns>Order fill information detailing the average price and quantity filled.</returns>
        OrderEvent MarketOnCloseFill(Security asset, MarketOnCloseOrder order);

        /// <summary>
        /// Slippage Model. Return a decimal cash slippage approximation on the order.
        /// </summary>
        decimal GetSlippageApproximation(Security asset, Order order);

        /// <summary>
        /// Gets the order fee associated with the specified order. This returns the cost
        /// of the transaction in the account currency
        /// </summary>
        /// <param name="security">The security matching the order</param>
        /// <param name="order">The order to compute fees for</param>
        /// <returns>The cost of the order in units of the account currency</returns>
        decimal GetOrderFee(Security security, Order order);

        /// <summary>
        /// Fee Model. Return the decimal fees from one order. Currently defaults to interactive
        /// </summary>
        /// <param name="quantity">Quantity for this Order</param>
        /// <param name="price">Average Price for this Order</param>
        /// <returns>Decimal value of the Order Fee</returns>
        [Obsolete("GetOrderFee(quantity, price) method has been made obsolete, use GetOrderFee(Security, Order) instead.")]
        decimal GetOrderFee(decimal quantity, decimal price);

        /// <summary>
        /// Perform neccessary check to see if the model has been filled, appoximate the best we can.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Order class to check if filled.</param>
        [Obsolete("Fill(Security, Order) method has been made obsolete, use fill methods directly instead (e.g. MarketFill(security, marketOrder)).")]
        OrderEvent Fill(Security asset, Order order);

        /// <summary>
        /// Model the slippage on a market order: fixed percentage of order price
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Order to update</param>
        [Obsolete("MarketFill(Security, Order) method has been made obsolete, use MarketFill(Security, MarketOrder) method instead.")]
        OrderEvent MarketFill(Security asset, Order order);

        /// <summary>
        /// Check if the model has stopped out our position yet: (Stop Market Order Type)
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Stop Order to Check, return filled if true</param>
        [Obsolete("StopFill(Security, Order) method has been made obsolete, use StopMarketFill(Security, StopMarketOrder) method instead.")]
        OrderEvent StopFill(Security asset, Order order);

        /// <summary>
        /// Model for a limit fill.
        /// </summary>
        /// <param name="asset">Stock Object to use to help model limit fill</param>
        /// <param name="order">Order to fill. Alter the values directly if filled.</param>
        [Obsolete("LimitFill(Security, Order) method has been made obsolete, use LimitFill(Security, LimitOrder) method instead.")]
        OrderEvent LimitFill(Security asset, Order order);
    }
}
