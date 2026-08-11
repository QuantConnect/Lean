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

using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Orders
{
    /// <summary>
    /// The collection of leg order tickets resulting from a combo order submission,
    /// with helpers to track the combo as a single unit instead of reassembling the
    /// legs by group order manager id
    /// </summary>
    /// <remarks>Deliberately a non-generic <see cref="List{T}"/> subclass: pythonnet converts
    /// generic list instances into plain Python lists, which would strip these properties.
    /// A non-generic subclass reaches Python as an object that still supports len(),
    /// indexing and iteration</remarks>
    public class ComboOrderTicket : List<OrderTicket>
    {
        /// <summary>
        /// The order tickets of the combo order legs
        /// </summary>
        public IReadOnlyList<OrderTicket> Tickets => this;

        /// <summary>
        /// The unique id of the group of orders this combo order consists of, null if empty
        /// </summary>
        public int? GroupOrderManagerId => Count > 0 ? this[0].SubmitRequest?.GroupOrderManager?.Id : null;

        /// <summary>
        /// True if every leg of the combo order has been completely filled
        /// </summary>
        public bool Filled => Count > 0 && this.All(ticket => ticket.Status == OrderStatus.Filled);

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        public ComboOrderTicket()
        {
        }

        /// <summary>
        /// Creates a new instance holding the given leg order tickets
        /// </summary>
        /// <param name="tickets">The order tickets of the combo order legs</param>
        public ComboOrderTicket(IEnumerable<OrderTicket> tickets) : base(tickets)
        {
        }
    }
}
