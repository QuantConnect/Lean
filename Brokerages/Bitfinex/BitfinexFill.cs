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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Tracks fill messages
    /// </summary>
    public class BitfinexFill
    {

        Orders.Order _order;
        decimal _scaleFactor;

        /// <summary>
        /// Lean orderId
        /// </summary>
        public int OrderId
        {
            get
            {
                return _order.Id;
            }
        }

        Dictionary<int, TradeMessage> messages = new Dictionary<int, TradeMessage>();

        /// <summary>
        /// Creates instance of BitfinexFill
        /// </summary>
        /// <param name="order"></param>
        /// <param name="scaleFactor"></param>
        public BitfinexFill(Orders.Order order, decimal scaleFactor)
        {
            _order = order;
            _scaleFactor = scaleFactor;
        }

        /// <summary>
        /// Adds a trade message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Add(TradeMessage msg)
        {
            if (!messages.ContainsKey(msg.TRD_ID))
            {
                messages.Add(msg.TRD_ID, msg);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compares fill amouns to determine if fill is complete
        /// </summary>
        /// <returns></returns>
        public bool IsCompleted()
        {
            decimal quantity = messages.Sum(m => m.Value.TRD_AMOUNT_EXECUTED) * _scaleFactor;
            return quantity >= _order.Quantity;
        }

        /// <summary>
        /// The total fee across all fills
        /// </summary>
        /// <returns></returns>
        public decimal TotalFee()
        {
            return messages.Sum(m => m.Value.FEE);
        }

        /// <summary>
        /// Total amount executed across all fills
        /// </summary>
        /// <returns></returns>
        public decimal TotalQuantity()
        {
            return messages.Sum(m => m.Value.TRD_AMOUNT_EXECUTED);
        }


    }
}
