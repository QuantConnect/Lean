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
    /// Defines the result for <see cref="IFillModel.Fill"/>
    /// </summary>
    public interface IFill { }

    /// <summary>
    /// Defines a possible result for <see cref="IFillModel.Fill"/> for a single order
    /// </summary>
    public class Fill : IFill
    {
        /// <summary>
        /// The order event associated to this <see cref="Fill"/> instance
        /// </summary>
        public OrderEvent OrderEvent { get; }

        /// <summary>
        /// Creates a new <see cref="Fill"/> instance
        /// </summary>
        /// <param name="orderEvent"></param>
        public Fill(OrderEvent orderEvent)
        {
            OrderEvent = orderEvent;
        }
    }

    /// <summary>
    /// Defines a possible result for <see cref="IFillModel.Fill"/> for combo orders
    /// </summary>
    public class ComboFill : IFill, IEnumerable<OrderEvent>
    {
        private readonly List<OrderEvent> _orderEvents = new();

        /// <summary>
        /// Creates a new <see cref="ComboFill"/> instance
        /// </summary>
        /// <param name="orderEvents">The fill order events for each order in the combo</param>
        public ComboFill(IEnumerable<OrderEvent> orderEvents)
        {
            _orderEvents.AddRange(orderEvents);
        }

        /// <summary>
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
