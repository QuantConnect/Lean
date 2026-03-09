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

namespace QuantConnect.Orders
{
    /// <summary>
    /// Event that fires each time an order is updated in the brokerage side.
    /// These are not status changes but mainly price changes, like the stop price of a trailing stop order.
    /// </summary>
    public class OrderUpdateEvent
    {
        /// <summary>
        /// The order ID.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// The updated stop price for a <see cref="TrailingStopOrder"/>
        /// </summary>
        public decimal TrailingStopPrice { get; set; }

        /// <summary>
        /// Flag indicating whether stop has been triggered for a <see cref="StopLimitOrder"/>
        /// </summary>
        public bool StopTriggered { get; set; }
    }
}
