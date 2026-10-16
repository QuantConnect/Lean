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
 *
*/

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides an implementation of the <see cref="OrderProperties"/> specific to Alpaca order.
    /// </summary>
    public class AlpacaOrderProperties : OrderProperties
    {
        /// <summary>
        /// Flag to allow orders to also trigger or fill outside of regular trading hours.
        /// </summary>
        public bool OutsideRegularTradingHours { get; set; }

        /// <summary>
        /// The id the algorithm gives the order, sent to Alpaca as its client order id. Alpaca generates one when it is not set.
        /// The brokerage reads it back on the orders it loads at startup, so an algorithm can match them to its own records after a restart.
        /// It must be unique in the Alpaca account, up to 128 characters.
        /// </summary>
        public string ClientOrderId { get; set; }
    }
}
