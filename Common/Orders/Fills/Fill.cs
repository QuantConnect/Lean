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

using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Defines a possible result for <see cref="IFillModel.Fill"/> for a single order
    /// </summary>
    public class Fill : IEnumerable<OrderEvent>
    {
        private readonly List<OrderEvent> _orderEvents;

        /// <summary>
        /// Creates a new <see cref="Fill"/> instance
        /// </summary>
        /// <param name="orderEvents">The fill order events</param>
        public Fill(List<OrderEvent> orderEvents)
        {
            _orderEvents = orderEvents;
        }

        /// <summary>
        /// Creates a new <see cref="Fill"/> instance
        /// </summary>
        /// <param name="orderEvent">The fill order event</param>
        public Fill(OrderEvent orderEvent)
        {
            _orderEvents = new() { orderEvent };
        }

        /// <summary>
        /// Returns the order events enumerator
        /// </summary>
        public IEnumerator<OrderEvent> GetEnumerator()
        {
            return _orderEvents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
