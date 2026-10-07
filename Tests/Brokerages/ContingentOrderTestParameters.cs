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
using System.Linq;
using QuantConnect.Orders;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages
{
    /// <summary>
    /// A set of contingent orders (OCO, OUO, OTO, brackets and their compositions) for the brokerage tests, made of the orders
    /// of other test parameters so any order type, symbol and shape can be tested
    /// </summary>
    public class ContingentOrderTestParameters
    {
        private readonly string _name;
        private readonly Func<decimal, List<Order>> _createOrders;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="name">The name of the test case</param>
        /// <param name="createOrders">Creates the related orders of the set for the given quantity, parents before the orders they trigger</param>
        public ContingentOrderTestParameters(string name, Func<decimal, List<Order>> createOrders)
        {
            _name = name;
            _createOrders = createOrders;
        }

        /// <summary>
        /// Creates the orders of the set, parents before the orders they trigger
        /// </summary>
        public List<Order> CreateOrders(decimal quantity)
        {
            return _createOrders(quantity);
        }

        /// <summary>
        /// Long orders where the first one to fill cancels the rest
        /// </summary>
        public static ContingentOrderTestParameters OneCancelsOther(params OrderTestParameters[] members)
        {
            return Related(ContingencyType.OneCancelsOther, members);
        }

        /// <summary>
        /// Long orders where a fill of one reduces the rest proportionally
        /// </summary>
        public static ContingentOrderTestParameters OneUpdatesOther(params OrderTestParameters[] members)
        {
            return Related(ContingencyType.OneUpdatesOther, members);
        }

        /// <summary>
        /// A long order which once filled triggers the short orders, held until then
        /// </summary>
        public static ContingentOrderTestParameters OneTriggersOther(OrderTestParameters parent, params OrderTestParameters[] children)
        {
            return new($"{ContingencyType.OneTriggersOther} {parent} -> [{string.Join(", ", children.Select(x => x))}]", quantity =>
            {
                var parentOrder = parent.CreateLongOrder(quantity);
                var childOrders = children.Select(child => child.CreateShortOrder(quantity)).ToList();
                OrderContingency.Trigger([parentOrder], childOrders);
                return [parentOrder, .. childOrders];
            });
        }

        /// <summary>
        /// A long entry which once filled triggers a short take profit and a short stop loss, where the first one to fill cancels the other
        /// </summary>
        public static ContingentOrderTestParameters Bracket(OrderTestParameters entry, OrderTestParameters takeProfit, OrderTestParameters stopLoss)
        {
            return new($"Bracket {entry} -> [{takeProfit}, {stopLoss}]", quantity =>
            {
                var entryOrder = entry.CreateLongOrder(quantity);
                var exits = new List<Order> { takeProfit.CreateShortOrder(quantity), stopLoss.CreateShortOrder(quantity) };
                OrderContingency.Trigger([entryOrder], exits);
                OrderContingency.Relate(ContingencyType.OneCancelsOther, exits);
                return [entryOrder, .. exits];
            });
        }

        private static ContingentOrderTestParameters Related(ContingencyType type, OrderTestParameters[] members)
        {
            return new($"{type} [{string.Join(", ", members.Select(x => x))}]", quantity =>
            {
                var orders = members.Select(member => member.CreateLongOrder(quantity)).ToList();
                OrderContingency.Relate(type, orders);
                return orders;
            });
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
