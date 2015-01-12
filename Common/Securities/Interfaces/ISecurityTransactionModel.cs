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

using QuantConnect.Orders;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;

namespace QuantConnect.Securities.Interfaces 
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Security transaction model interface for QuantConnect security objects
    /// </summary>
    /// <seealso cref="EquityTransactionModel"/>
    /// <seealso cref="ForexTransactionModel"/>
    public interface ISecurityTransactionModel
    {
        /******************************************************** 
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// Perform neccessary check to see if the model has been filled, appoximate the best we can.
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Order class to check if filled.</param>
        OrderEvent Fill(Security asset, Order order);


        /// <summary>
        /// Get the Slippage approximation for this order:
        /// </summary>
        decimal GetSlippageApproximation(Security asset, Order order);


        /// <summary>
        /// Model the slippage on a market order: fixed percentage of order price
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Order to update</param>
        OrderEvent MarketFill(Security asset, Order order);


        /// <summary>
        /// Check if the model has stopped out our position yet:
        /// </summary>
        /// <param name="asset">Asset we're trading this order</param>
        /// <param name="order">Stop Order to Check, return filled if true</param>
        OrderEvent StopFill(Security asset, Order order);


        /// <summary>
        /// Model for a limit fill.
        /// </summary>
        /// <param name="asset">Stock Object to use to help model limit fill</param>
        /// <param name="order">Order to fill. Alter the values directly if filled.</param>
        OrderEvent LimitFill(Security asset, Order order);


        /// <summary>
        /// Get the fees from one order. Currently defaults to interactive
        /// </summary>
        /// <param name="quantity">Quantity for this Order</param>
        /// <param name="price">Average Price for this Order</param>
        /// <returns>Decimal value of the Order Fee</returns>
        decimal GetOrderFee(decimal quantity, decimal price);

    } // End Algorithm Transaction Model Interface

} // End QC Namespace
