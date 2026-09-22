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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Orders
{
    /// <summary>
    /// The contingency of an order: the set of contingent orders it belongs to (OCO, OTO, OUO and any composition of
    /// them, like brackets) and the <see cref="Links"/> defining how this order relates to the rest of the set
    /// </summary>
    /// <remarks>
    /// The set state (<see cref="Id"/>, <see cref="Count"/>, <see cref="OrderIds"/>) is shared by all the orders of the set,
    /// see <see cref="WithLinks"/>, while the links, including their triggered state, belong to each order.
    /// Unlike a <see cref="GroupOrderManager"/> the orders of the set are independent, only their lifecycle is related
    /// </remarks>
    public class OrderContingency
    {
        private SharedState _set;
        private readonly List<ContingencyLink> _links;
        // the order this contingency belongs to, null for an order request
        private Order _order;

        /// <summary>
        /// The unique id of the set of contingent orders this order belongs to
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id => _set.Id;

        /// <summary>
        /// The total order count in the set of contingent orders
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count => _set.Count;

        /// <summary>
        /// The ids of the orders in the set
        /// </summary>
        /// <remarks>In live trading we process orders in dedicated threads so we need to be thread safe, access is synchronized locking this collection</remarks>
        [JsonProperty(PropertyName = "orderIds")]
        public HashSet<int> OrderIds => _set.OrderIds;

        /// <summary>
        /// The different symbols of the orders in the set. Allows a brokerage model to validate a single order
        /// knowing about the rest of the set. Only available at submission time
        /// </summary>
        [JsonIgnore]
        public IReadOnlySet<Symbol> Symbols => _set.Symbols ??= _set.BuildSet(member => member.Symbol);

        /// <summary>
        /// The different directions of the orders in the set. Allows a brokerage model to validate a single order
        /// knowing about the rest of the set. Only available at submission time
        /// </summary>
        [JsonIgnore]
        public IReadOnlySet<OrderDirection> Directions => _set.Directions ??= _set.BuildSet(member => member.Quantity > 0 ? OrderDirection.Buy : OrderDirection.Sell);

        /// <summary>
        /// The different order types of the orders in the set. Allows a brokerage model to validate a single order
        /// knowing about the rest of the set. Only available at submission time
        /// </summary>
        [JsonIgnore]
        public IReadOnlySet<OrderType> OrderTypes => _set.OrderTypes ??= _set.BuildSet(member => member.OrderType);

        /// <summary>
        /// The links of this order to the rest of the set: the role it plays in each contingency
        /// </summary>
        [JsonProperty(PropertyName = "links")]
        public IReadOnlyList<ContingencyLink> Links => _links;

        /// <summary>
        /// True if this is a contingent child order still open and held, waiting for its parent order to fill
        /// </summary>
        [JsonIgnore]
        public bool IsWaitingForTrigger => (_order == null || !_order.Status.IsClosed()) && GetLink(ContingencyRole.Child) is { Triggered: false };

        /// <summary>
        /// The order requests of the set before being submitted, in submission order: parents before the orders they trigger.
        /// See <see cref="OrderFactory"/>
        /// </summary>
        internal IReadOnlyList<SubmitOrderRequest> Requests => _set.Requests ??= _set.BuildRequests();

        /// <summary>
        /// Creates the contingency of the first order of a new set of contingent orders, the rest are created through <see cref="WithLinks"/>
        /// </summary>
        /// <param name="id">The unique id of the set of contingent orders</param>
        /// <param name="count">The total order count in the set</param>
        /// <param name="links">The links of this order to the rest of the set</param>
        public OrderContingency(int id, int count, IEnumerable<ContingencyLink> links)
            : this(new SharedState(id, count), links?.ToList() ?? new List<ContingencyLink>())
        {
        }

        /// <summary>
        /// Creates the contingency of the first order of a new set of contingent orders, the rest are created through <see cref="WithLinks"/>.
        /// The set id is assigned once the orders are added into the algorithm
        /// </summary>
        /// <param name="count">The total order count in the set</param>
        /// <param name="links">The links of this order to the rest of the set</param>
        public OrderContingency(int count, IEnumerable<ContingencyLink> links)
            : this(0, count, links)
        {
        }

        /// <summary>
        /// Creates a new instance from its serialized form, the set is not shared with any other instance
        /// </summary>
        [JsonConstructor]
        private OrderContingency(int id, int count, IEnumerable<int> orderIds, IEnumerable<ContingencyLink> links)
            : this(id, count, links)
        {
            if (orderIds != null)
            {
                _set.OrderIds.UnionWith(orderIds);
            }
        }

        private OrderContingency(SharedState set, List<ContingencyLink> links)
        {
            _set = set;
            _links = links;
        }

        /// <summary>
        /// Gets the first link with the given role: parent or child of a <see cref="ContingencyType.OneTriggersOther"/> contingency,
        /// or null for the link to the siblings of a <see cref="ContingencyType.OneCancelsOther"/>/<see cref="ContingencyType.OneUpdatesOther"/> one
        /// </summary>
        internal ContingencyLink GetLink(ContingencyRole? role)
        {
            for (var i = 0; i < _links.Count; i++)
            {
                if (_links[i].Role == role)
                {
                    return _links[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Creates the contingency of another order of the same set of contingent orders: it shares the set with this instance,
        /// with the given links of its own
        /// </summary>
        /// <param name="links">The links of the other order to the rest of the set</param>
        public OrderContingency WithLinks(IEnumerable<ContingencyLink> links)
        {
            return new OrderContingency(_set, links?.ToList() ?? new List<ContingencyLink>());
        }

        /// <summary>
        /// Creates a copy of this instance: the set is shared, the links are cloned
        /// </summary>
        public OrderContingency Clone()
        {
            var links = new List<ContingencyLink>(_links.Count);
            for (var i = 0; i < _links.Count; i++)
            {
                links.Add(_links[i].Clone());
            }
            return new OrderContingency(_set, links);
        }

        /// <summary>
        /// Returns a string that represents the current object
        /// </summary>
        public override string ToString()
        {
            return $"Set {Id.ToStringInvariant()} ({Count.ToStringInvariant()}): [{string.Join(",", _links)}]";
        }

        /// <summary>
        /// Sets the unique id of the set of contingent orders, once the orders are added into the algorithm
        /// </summary>
        /// <param name="id">The unique id of the set</param>
        internal void SetId(int id)
        {
            _set.Id = id;
        }

        /// <summary>
        /// Sets the order this contingency belongs to
        /// </summary>
        internal void SetOrder(Order order)
        {
            _order = order;
        }

        /// <summary>
        /// The order types of the parents of this order, the orders it's waiting for, all the legs for a combo order
        /// </summary>
        internal IEnumerable<OrderType> GetParentOrderTypes()
        {
            var child = GetLink(ContingencyRole.Child);
            if (child == null)
            {
                yield break;
            }
            foreach (var member in _set.Members)
            {
                var links = member.Contingency?.Links;
                if (links == null)
                {
                    continue;
                }
                for (var i = 0; i < links.Count; i++)
                {
                    if (links[i].Role == ContingencyRole.Parent && links[i].Id == child.Id)
                    {
                        yield return member.OrderType;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Relates the parent order to the orders it triggers once it completely fills (One Triggers Other)
        /// </summary>
        /// <param name="parent">The parent order, all the legs for a combo order</param>
        /// <param name="children">The orders to trigger, all the legs for combo orders</param>
        internal static void Trigger(IEnumerable<SubmitOrderRequest> parent, IEnumerable<SubmitOrderRequest> children)
        {
            Link(ContingencyType.OneTriggersOther, (Members(parent), ContingencyRole.Parent), (Members(children), ContingencyRole.Child));
        }

        /// <summary>
        /// Relates the orders to each other as siblings: One Cancels Other or One Updates Other
        /// </summary>
        /// <param name="type">The type of the relation</param>
        /// <param name="members">The orders to relate, all the legs for combo orders</param>
        internal static void Relate(ContingencyType type, IEnumerable<SubmitOrderRequest> members)
        {
            Link(type, (Members(members), null));
        }

        /// <summary>
        /// Helper for brokerages to rebuild the contingencies of their open orders: relates the parent order to the orders it triggers once
        /// it completely fills (One Triggers Other), joining them into a single set of contingent orders
        /// </summary>
        /// <param name="parent">The parent order, all the legs for a combo order</param>
        /// <param name="children">The orders to trigger, all the legs for combo orders</param>
        public static void Trigger(IEnumerable<Order> parent, IEnumerable<Order> children)
        {
            Link(ContingencyType.OneTriggersOther, (Members(parent), ContingencyRole.Parent), (Members(children), ContingencyRole.Child));
        }

        /// <summary>
        /// Helper for brokerages to rebuild the contingencies of their open orders: relates the orders to each other as siblings,
        /// One Cancels Other or One Updates Other, joining them into a single set of contingent orders
        /// </summary>
        /// <param name="type">The type of the relation</param>
        /// <param name="members">The orders to relate, all the legs for combo orders</param>
        public static void Relate(ContingencyType type, IEnumerable<Order> members)
        {
            Link(type, (Members(members), null));
        }

        /// <summary>
        /// Groups the orders into units, preserving their order: each order on its own except for the legs of a combo order which go together
        /// </summary>
        /// <exception cref="ArgumentException">An order is missing or repeated, or some legs of a combo order are missing</exception>
        public static List<List<Order>> GetUnits(IEnumerable<Order> orders)
        {
            return GetUnits(Members(orders)).Select(unit => unit.Select(member => (Order)member.Value).ToList()).ToList();
        }

        private static IEnumerable<Member> Members(IEnumerable<SubmitOrderRequest> requests)
        {
            return requests?.Select(request => new Member(request));
        }

        private static IEnumerable<Member> Members(IEnumerable<Order> orders)
        {
            return orders?.Select(order => new Member(order));
        }

        /// <summary>
        /// Relates the orders of each side through a new contingency, joining them into a single set of contingent orders
        /// </summary>
        /// <param name="type">The type of the contingency</param>
        /// <param name="sides">The orders playing each role in the contingency</param>
        private static void Link(ContingencyType type, params (IEnumerable<Member> Orders, ContingencyRole? Role)[] sides)
        {
            var units = new List<List<Member>>[sides.Length];
            for (var i = 0; i < sides.Length; i++)
            {
                var role = sides[i].Role;
                units[i] = GetUnits(sides[i].Orders);
                if (role == ContingencyRole.Parent ? units[i].Count != 1 : units[i].Count < (role == null ? 2 : 1))
                {
                    throw new ArgumentException($"Expected {(role == null ? "at least two orders to relate" : role == ContingencyRole.Parent ? "a single parent order" : "at least one order to trigger")}, all the legs for combo orders");
                }
                // a parent can trigger orders more than once, the rest of the roles are played once
                if (role != ContingencyRole.Parent)
                {
                    foreach (var unit in units[i])
                    {
                        if (unit[0].Contingency?.GetLink(role) != null)
                        {
                            throw new ArgumentException($"The orders are already {(role == null ? "related to other orders" : "triggered by another order")}");
                        }
                    }
                }
            }

            // the first side joins first, so the contingency ids follow the composition order
            var set = Join(null, units[0]);
            var contingencyId = ++set.NextContingencyId;
            for (var i = 0; i < sides.Length; i++)
            {
                var role = sides[i].Role;
                Join(set, units[i]);
                foreach (var unit in units[i])
                {
                    foreach (var leg in unit)
                    {
                        // the link to the parent goes first
                        var links = leg.Contingency._links;
                        links.Insert(role == ContingencyRole.Child ? 0 : links.Count, new ContingencyLink(contingencyId, type, role));
                    }
                }
            }
        }

        /// <summary>
        /// Groups the orders into units, preserving their order: each order on its own except for the legs of a combo order which go together
        /// </summary>
        /// <exception cref="ArgumentException">An order is missing or repeated, was already submitted, or some legs of a combo order are missing</exception>
        private static List<List<Member>> GetUnits(IEnumerable<Member> orders)
        {
            var units = new List<List<Member>>();
            var seen = new HashSet<object>();
            Dictionary<GroupOrderManager, List<Member>> comboUnits = null;
            foreach (var order in orders ?? Enumerable.Empty<Member>())
            {
                if (order.Value == null)
                {
                    throw new ArgumentException("Unexpected null order");
                }
                if (order.Value is SubmitOrderRequest { OrderId: > 0 })
                {
                    throw new ArgumentException($"The order was already submitted, it can only be submitted once: {order}");
                }
                if (!seen.Add(order.Value))
                {
                    throw new ArgumentException($"The order is present more than once: {order}");
                }

                if (order.GroupOrderManager == null)
                {
                    units.Add(new List<Member> { order });
                    continue;
                }
                comboUnits ??= new();
                if (!comboUnits.TryGetValue(order.GroupOrderManager, out var unit))
                {
                    comboUnits[order.GroupOrderManager] = unit = new List<Member>();
                    units.Add(unit);
                }
                unit.Add(order);
            }

            if (comboUnits != null)
            {
                foreach (var (groupOrderManager, legs) in comboUnits)
                {
                    if (legs.Count != groupOrderManager.Count)
                    {
                        throw new ArgumentException($"Expected all the {groupOrderManager.Count} legs of the combo order, got {legs.Count}: {legs[0]}");
                    }
                }
            }
            return units;
        }

        /// <summary>
        /// Joins the units into the given set of contingent orders, if none the one of the first unit which belongs to a set or a new one.
        /// The contingency ids of the sets which join remain unique, they are shifted
        /// </summary>
        private static SharedState Join(SharedState set, IEnumerable<List<Member>> units)
        {
            foreach (var unit in units)
            {
                var contingency = unit[0].Contingency;
                if (contingency == null)
                {
                    set ??= new SharedState(0, 0);
                    foreach (var leg in unit)
                    {
                        // an exercise is an instruction, not a working order which can be held, canceled or resized
                        if (leg.OrderType == OrderType.OptionExercise)
                        {
                            throw new ArgumentException($"Option exercise orders can not be part of a set of contingent orders: {leg}");
                        }
                        leg.Contingency = new OrderContingency(set, new List<ContingencyLink>());
                        set.Members.Add(leg);
                    }
                    set.OnMembersChanged();
                }
                else if (set == null)
                {
                    set = contingency._set;
                }
                else if (!ReferenceEquals(set, contingency._set))
                {
                    var other = contingency._set;
                    var offset = set.NextContingencyId;
                    foreach (var member in other.Members)
                    {
                        var links = member.Contingency._links;
                        var shiftedLinks = new List<ContingencyLink>(links.Count);
                        for (var i = 0; i < links.Count; i++)
                        {
                            shiftedLinks.Add(new ContingencyLink(links[i].Id + offset, links[i].Type, links[i].Role));
                        }
                        member.Contingency = new OrderContingency(set, shiftedLinks);
                        set.Members.Add(member);
                    }
                    set.NextContingencyId += other.NextContingencyId;
                    set.OnMembersChanged();
                }
            }
            return set;
        }

        /// <summary>
        /// A member of a set of contingent orders: an order request before being submitted, or an order
        /// </summary>
        private readonly struct Member
        {
            private readonly SubmitOrderRequest _request;
            private readonly Order _order;

            public Member(SubmitOrderRequest request)
            {
                _request = request;
            }

            public Member(Order order)
            {
                _order = order;
            }

            public object Value => _request ?? (object)_order;
            public GroupOrderManager GroupOrderManager => _request != null ? _request.GroupOrderManager : _order.GroupOrderManager;
            public OrderType OrderType => _request?.OrderType ?? _order.Type;
            public Symbol Symbol => _request != null ? _request.Symbol : _order.Symbol;
            public decimal Quantity => _request?.Quantity ?? _order.Quantity;

            public OrderContingency Contingency
            {
                get => _request != null ? _request.Contingency : _order.Contingency;
                set
                {
                    if (_request != null)
                    {
                        _request.Contingency = value;
                    }
                    else
                    {
                        _order.Contingency = value;
                    }
                }
            }

            public override string ToString()
            {
                return Value?.ToString();
            }
        }

        /// <summary>
        /// The state of a set of contingent orders, a single instance is shared by the contingencies of all the orders in the set
        /// </summary>
        private class SharedState
        {
            public int Id { get; set; }
            public int Count { get; set; }
            public HashSet<int> OrderIds { get; }
            public List<Member> Members { get; } = new();
            public int NextContingencyId { get; set; }

            // views of the members, built lazily on first use
            public HashSet<Symbol> Symbols { get; set; }
            public HashSet<OrderDirection> Directions { get; set; }
            public HashSet<OrderType> OrderTypes { get; set; }
            public List<SubmitOrderRequest> Requests { get; set; }

            public SharedState(int id, int count)
            {
                Id = id;
                Count = count;
                OrderIds = new(capacity: Math.Max(count, 0));
            }

            public void OnMembersChanged()
            {
                Count = Members.Count;
                Symbols = null;
                Directions = null;
                OrderTypes = null;
                Requests = null;
            }

            public HashSet<T> BuildSet<T>(Func<Member, T> selector)
            {
                var result = new HashSet<T>();
                foreach (var member in Members)
                {
                    result.Add(selector(member));
                }
                return result;
            }

            public List<SubmitOrderRequest> BuildRequests()
            {
                var result = new List<SubmitOrderRequest>(Members.Count);
                foreach (var member in Members)
                {
                    if (member.Value is SubmitOrderRequest request)
                    {
                        result.Add(request);
                    }
                }
                return result;
            }
        }
    }
}
