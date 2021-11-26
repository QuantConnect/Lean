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

using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Represents a binance submit order event data
    /// </summary>
    public class BinanceOrderSubmitEventArgs
    {
        /// <summary>
        /// Order Event Constructor.
        /// </summary>
        /// <param name="brokerId">Binance order id returned from brokerage</param>
        /// <param name="order">Order for this order placement</param>
        public BinanceOrderSubmitEventArgs(string brokerId, Order order)
        {
            BrokerId = brokerId;
            Order = order;
        }

        /// <summary>
        /// Original brokerage id
        /// </summary>
        public string BrokerId { get; set; }

        /// <summary>
        /// The lean order
        /// </summary>
        public Order Order { get; set; }
    }
}
