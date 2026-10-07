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
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Defines the lifecycle rules of contingent orders (OCO, OTO, OUO and their compositions, like brackets).
    /// Given the order events that happened it determines which orders should be triggered, canceled or resized,
    /// it's up to the caller, the one simulating the brokerage side, to apply these actions.
    /// </summary>
    /// <remarks>
    /// The rules are:
    ///  - <see cref="ContingencyType.OneTriggersOther"/>: children are held until their parent, all its legs for a combo order,
    ///  is completely filled. If the parent is canceled, even if partially filled, or turns invalid its children are canceled.
    ///  - <see cref="ContingencyType.OneCancelsOther"/>: the first fill of a member, even if partial, cancels its siblings.
    ///  - <see cref="ContingencyType.OneUpdatesOther"/>: a partial fill of a member reduces the remaining quantity of its siblings
    ///  proportionally, once it's completely filled its siblings are canceled.
    /// The legs of a combo order are handled as a single unit. This type holds no state so it's thread safe.
    /// </remarks>
    internal class ContingentOrderProcessor
    {
        private readonly Func<int, decimal> _filledQuantityProvider;
        private readonly ISecurityProvider _securityProvider;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="filledQuantityProvider">Provides the total filled quantity of an order by id</param>
        /// <param name="securityProvider">The security provider to use</param>
        public ContingentOrderProcessor(Func<int, decimal> filledQuantityProvider, ISecurityProvider securityProvider)
        {
            _filledQuantityProvider = filledQuantityProvider;
            _securityProvider = securityProvider;
        }

        /// <summary>
        /// Determines the actions to take on the contingent orders related to the orders of the given events
        /// </summary>
        /// <param name="orderEvents">The order events that happened, already applied</param>
        /// <param name="orderProvider">Provides access to the orders by id, null if it does not exist</param>
        /// <param name="utcTime">The current utc time, for the events</param>
        /// <returns>The updates of the orders to trigger or resize, and the events of the orders to cancel. Null if none</returns>
        public (List<OrderUpdateEvent> Updates, List<OrderEvent> Cancels) Process(IEnumerable<OrderEvent> orderEvents, Func<int, Order> orderProvider,
            DateTime utcTime)
        {
            var actions = new Actions(utcTime, _securityProvider);
            List<Order> contingentOrders = null;
            foreach (var orderEvent in orderEvents)
            {
                if (!orderEvent.Status.IsClosed() && orderEvent.Status != OrderStatus.PartiallyFilled)
                {
                    continue;
                }

                var order = orderProvider(orderEvent.OrderId);
                if (order == null || !order.IsContingent())
                {
                    continue;
                }

                // the events of the same set, like the legs of a combo order, usually come together: the set is fetched once
                if (contingentOrders == null || !ReferenceEquals(contingentOrders[0].Contingency?.OrderIds, order.Contingency.OrderIds))
                {
                    contingentOrders = order.GetExistingContingentOrders(orderProvider);
                }

                if (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.PartiallyFilled)
                {
                    ProcessFill(order, orderEvent, contingentOrders, actions);
                    continue;
                }

                // the parent was canceled or turned invalid: it won't ever trigger its children. A member was canceled or turned
                // invalid: the contingency is canceled as a whole, like brokerages do
                ProcessHeldChildren(order, contingentOrders, orderEvent.Status, actions);
                CancelSiblings(order, order.GetSiblingLink(), contingentOrders, orderEvent.Status, actions);
            }
            return (actions.Updates, actions.Cancels);
        }

        /// <summary>
        /// Triggers the children of the given parent still held waiting for it to fill, or cancels them if the parent was closed
        /// </summary>
        /// <param name="order">The parent order</param>
        /// <param name="contingentOrders">The orders in the set</param>
        /// <param name="parentClosedStatus">The status of the parent if it was closed without filling, null if it filled</param>
        /// <param name="actions">The actions to add to</param>
        private static void ProcessHeldChildren(Order order, List<Order> contingentOrders, OrderStatus? parentClosedStatus, Actions actions)
        {
            var parent = order.GetContingencyLink(ContingencyRole.Parent);
            if (parent == null)
            {
                return;
            }
            foreach (var other in contingentOrders)
            {
                var child = other.Id != order.Id && !other.Status.IsClosed() ? other.Contingency?.GetLink(ContingencyRole.Child) : null;
                if (child == null || child.Id != parent.Id || child.Triggered)
                {
                    continue;
                }
                if (parentClosedStatus == null)
                {
                    actions.Trigger(other);
                }
                else
                {
                    actions.Cancel(other, $"Contingent parent order {order.Id} was {parentClosedStatus.Value.ToString().ToLowerInvariant()}");
                }
            }
        }

        /// <summary>
        /// Cancels the siblings of the given order which are still open
        /// </summary>
        /// <param name="order">The order which was filled or closed</param>
        /// <param name="member">The link of the order to its siblings</param>
        /// <param name="contingentOrders">The orders in the set</param>
        /// <param name="status">The status of the order, the reason of the cancelation</param>
        /// <param name="actions">The actions to add to</param>
        private static void CancelSiblings(Order order, ContingencyLink member, List<Order> contingentOrders, OrderStatus status, Actions actions)
        {
            if (member == null)
            {
                return;
            }
            foreach (var sibling in contingentOrders)
            {
                if (!sibling.Status.IsClosed() && order.IsContingentSibling(member, sibling))
                {
                    actions.Cancel(sibling, $"Contingent sibling order {order.Id} was {status.ToString().ToLowerInvariant()}");
                }
            }
        }

        private void ProcessFill(Order order, OrderEvent orderEvent, List<Order> contingentOrders, Actions actions)
        {
            var completelyFilled = orderEvent.Status == OrderStatus.Filled;

            var member = order.GetSiblingLink();
            if (member != null)
            {
                if (completelyFilled || member.Type == ContingencyType.OneCancelsOther)
                {
                    CancelSiblings(order, member, contingentOrders, OrderStatus.Filled, actions);
                }
                else if (orderEvent.FillQuantity != 0)
                {
                    // OUO partial fill: the remaining quantity of the siblings is reduced proportionally
                    var remainingAfter = Math.Abs(order.Quantity) - Math.Abs(_filledQuantityProvider(order.Id));
                    var remainingBefore = remainingAfter + Math.Abs(orderEvent.FillQuantity);
                    if (remainingBefore > 0 && remainingAfter >= 0)
                    {
                        foreach (var sibling in contingentOrders)
                        {
                            if (sibling.Status.IsClosed() || !order.IsContingentSibling(member, sibling))
                            {
                                continue;
                            }
                            var siblingFilled = Math.Abs(_filledQuantityProvider(sibling.Id));
                            // multiply first so we don't lose precision
                            var siblingRemaining = (Math.Abs(sibling.Quantity) - siblingFilled) * remainingAfter / remainingBefore;

                            var lotSize = _securityProvider?.GetSecurity(sibling.Symbol)?.SymbolProperties.LotSize ?? 0;
                            if (lotSize > 0)
                            {
                                siblingRemaining = Math.Round(siblingRemaining / lotSize) * lotSize;
                            }

                            if (siblingRemaining <= 0)
                            {
                                actions.Cancel(sibling, $"Contingent sibling order {order.Id} was filled");
                            }
                            else
                            {
                                var newQuantity = Math.Sign(sibling.Quantity) * (siblingFilled + siblingRemaining);
                                if (newQuantity != sibling.Quantity)
                                {
                                    actions.UpdateQuantity(sibling, newQuantity);
                                }
                            }
                        }
                    }
                }
            }

            var parent = order.GetContingencyLink(ContingencyRole.Parent);
            if (parent != null && completelyFilled)
            {
                // for combo orders all the legs have to be filled
                foreach (var other in contingentOrders)
                {
                    if (other.Id != order.Id && other.Status != OrderStatus.Filled && other.GetContingencyLink(ContingencyRole.Parent)?.Id == parent.Id)
                    {
                        return;
                    }
                }
                ProcessHeldChildren(order, contingentOrders, null, actions);
            }
        }

        /// <summary>
        /// Builds the events of the actions to take, at most one per order
        /// </summary>
        private class Actions
        {
            private readonly DateTime _utcTime;
            private readonly ISecurityProvider _securityProvider;
            private HashSet<int> _orderIds;

            public List<OrderUpdateEvent> Updates { get; private set; }
            public List<OrderEvent> Cancels { get; private set; }

            public Actions(DateTime utcTime, ISecurityProvider securityProvider)
            {
                _utcTime = utcTime;
                _securityProvider = securityProvider;
            }

            /// <summary>
            /// The held child is released to the market, a trailing stop starts trailing from the market price at this time
            /// </summary>
            public void Trigger(Order order)
            {
                if (Add(order))
                {
                    var update = new OrderUpdateEvent { OrderId = order.Id, ContingencyTriggered = true };
                    if (order is TrailingStopOrder { StopPrice: 0 } trailingStop && _securityProvider?.GetSecurity(order.Symbol) is { } security)
                    {
                        update.TrailingStopPrice = TrailingStopOrder.CalculateStopPrice(security.Price, trailingStop.TrailingAmount,
                            trailingStop.TrailingAsPercentage, trailingStop.Direction);
                    }
                    (Updates ??= new()).Add(update);
                }
            }

            public void Cancel(Order order, string message)
            {
                if (Add(order))
                {
                    (Cancels ??= new()).Add(new OrderEvent(order, _utcTime, OrderFee.Zero, message) { Status = OrderStatus.Canceled });
                }
            }

            public void UpdateQuantity(Order order, decimal quantity)
            {
                if (Add(order))
                {
                    (Updates ??= new()).Add(new OrderUpdateEvent { OrderId = order.Id, Quantity = quantity });
                }
            }

            private bool Add(Order order)
            {
                return (_orderIds ??= new()).Add(order.Id);
            }
        }
    }
}
