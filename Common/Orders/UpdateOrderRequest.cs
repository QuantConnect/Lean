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
    /// Request to update pending orders.
    /// </summary>
    public class UpdateOrderRequest :
        OrderRequest
    {
        /// <summary>
        /// Limit Price of the Limit and StopLimit Orders.
        /// </summary>
        public decimal LimitPrice;

        /// <summary>
        /// Stop Price of the StopLimit and StopMarket Orders.
        /// </summary>
        public decimal StopPrice;

        /// <summary>
        /// Number of shares to execute.
        /// </summary>
        public int Quantity;

        /// <summary>
        /// Tag the order with some custom data
        /// </summary>
        public string Tag;

        /// <summary>
        /// Update request description
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("[{0}] Request to update order [{1}] Quantity: {2} LimitPrice: {3} StopPrice: {4} Tag: {5}", Id, OrderId, Quantity, LimitPrice, StopPrice, Tag);
        }
    }
}
