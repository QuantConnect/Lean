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

using QuantConnect.Brokerages.GDAX.Messages;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// Tracks fill messages
    /// </summary>
    public class GDAXFill
    {
        private readonly List<Fill> _messages = new List<Fill>();

        /// <summary>
        /// The Lean order
        /// </summary>
        public Orders.Order Order { get; }

        /// <summary>
        /// Lean orderId
        /// </summary>
        public int OrderId => Order.Id;

        /// <summary>
        /// Total amount executed across all fills
        /// </summary>
        /// <returns></returns>
        public decimal TotalQuantity => _messages.Sum(m => m.Size);

        /// <summary>
        /// Original order quantity
        /// </summary>
        public decimal OrderQuantity => Order.Quantity;

        /// <summary>
        /// Creates instance of GDAXFill
        /// </summary>
        /// <param name="order"></param>
        public GDAXFill(Orders.Order order)
        {
            Order = order;
        }

        /// <summary>
        /// Adds a trade message
        /// </summary>
        /// <param name="msg"></param>
        public void Add(Fill msg)
        {
            _messages.Add(msg);
        }
    }
}
