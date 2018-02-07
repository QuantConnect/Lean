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

using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Bitfinex
{
    /// <summary>
    /// Tracks fill messages
    /// </summary>
    public class BitfinexFill
    {
        private readonly Orders.Order _order;
        private readonly IBrokerage _brokerage;

        /// <summary>
        /// Lean orderId
        /// </summary>
        public int OrderId => _order.Id;

        /// <summary>
        /// Original order quantity
        /// </summary>
        public decimal OrderQuantity => _order.Quantity;

        private readonly Dictionary<long, Messages.Fill> _messages = new Dictionary<long, Messages.Fill>();

        /// <summary>
        /// Creates instance of BitfinexFill
        /// </summary>
        /// <param name="order"></param>
        public BitfinexFill(Orders.Order order, IBrokerage brokerage)
        {
            _order = order;
            _brokerage = brokerage;
        }

        /// <summary>
        /// Adds a trade message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Add(Messages.Fill msg)
        {
            if (!_messages.ContainsKey(msg.Id))
            {
                _messages.Add(msg.Id, msg);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if order is complete
        /// </summary>
        /// <returns></returns>
        public bool IsCompleted()
        {
            var quantity = _messages.Sum(m => m.Value.AmountExecuted);
            if (Math.Abs(quantity) >= Math.Abs(_order.Quantity))
            {
                return true;
            }

            //For partial fills, brokerage sometimes has "feelsgood fee": order fill < order quantity
            //we'll double check if order is still open
            var open = _brokerage.GetOpenOrders().SelectMany(o => o.BrokerId);
            //todo: return missing fragements as fee

            return !_order.BrokerId.Intersect(open).Any();
        }

        /// <summary>
        /// The total fee across all fills
        /// </summary>
        /// <returns></returns>
        public decimal TotalFee()
        {
            return _messages.Sum(m => m.Value.Fee);
        }

        /// <summary>
        /// Total amount executed across all fills
        /// </summary>
        /// <returns></returns>
        public decimal TotalQuantity()
        {
            return _messages.Sum(m => m.Value.AmountExecuted);
        }

    }
}