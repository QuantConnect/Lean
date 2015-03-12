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

namespace QuantConnect.Orders
{
    /********************************************************
    * ORDER CLASS DEFINITION
    *********************************************************/
    /// <summary>
    /// Limit order type definition
    /// </summary>
    public class LimitOrder : Order
    {
        /// <summary>
        /// Limit price for this order.
        /// </summary>
        public decimal LimitPrice;

        /// <summary>
        /// Value of the order at limit price if a limit order
        /// </summary>
        public override decimal Value
        {
            get
            {
                return Convert.ToDecimal(Quantity) * LimitPrice;
            }
        }

        /// <summary>
        /// Added a default constructor for JSON Deserialization:
        /// </summary>
        public LimitOrder()
        {
            Type = OrderType.Limit;
        }

        /// <summary>
        /// New limit order constructor
        /// </summary>
        /// <param name="symbol">Symbol asset we're seeking to trade</param>
        /// <param name="type">Type of the security order</param>
        /// <param name="quantity">Quantity of the asset we're seeking to trade</param>
        /// <param name="time">Time the order was placed</param>
        /// <param name="limitPrice">Price the order should be filled at if a limit order</param>
        /// <param name="tag">User defined data tag for this order</param>
        public LimitOrder(string symbol, int quantity, decimal limitPrice, DateTime time, string tag = "", SecurityType type = SecurityType.Base)
            : base(symbol, quantity, OrderType.Limit, time, 0, tag, type)
        {
            LimitPrice = limitPrice;
            Type = OrderType.Limit;

            if (tag == "")
            {
                //Default tag values to display limit price in GUI.
                Tag = "Limit Price: " + limitPrice.ToString("C");
            }
        }
    }

} // End QC Namespace:
