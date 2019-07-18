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

namespace QuantConnect.Orders.Fills
{
    /// <summary>
    /// Defines the result for <see cref="IFillModel.Fill"/>
    /// </summary>
    public class Fill
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
}
