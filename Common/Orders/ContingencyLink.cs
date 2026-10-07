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
using Newtonsoft.Json;

namespace QuantConnect.Orders
{
    /// <summary>
    /// Links an order to a contingency, that is, a relationship with other orders of the same
    /// <see cref="OrderContingency"/>, and defines the role the order plays in it
    /// </summary>
    public class ContingencyLink
    {
        /// <summary>
        /// The contingency id, unique within its set of contingent orders.
        /// Orders sharing a contingency id are related through it
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; }

        /// <summary>
        /// The contingency type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public ContingencyType Type { get; }

        /// <summary>
        /// The role of the order in this contingency, for a <see cref="ContingencyType.OneTriggersOther"/> contingency.
        /// Null for the other types, whose orders are all siblings
        /// </summary>
        [JsonProperty(PropertyName = "role", NullValueHandling = NullValueHandling.Ignore)]
        public ContingencyRole? Role { get; }

        /// <summary>
        /// For a <see cref="ContingencyRole.Child"/>, whether the parent filled and so the order was released to the market
        /// </summary>
        [JsonProperty(PropertyName = "triggered", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Triggered { get; internal set; }

        /// <summary>
        /// For a <see cref="ContingencyRole.Child"/>, the utc time at which the order was triggered, if any
        /// </summary>
        [JsonProperty(PropertyName = "triggeredTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? TriggeredTime { get; internal set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="id">The contingency id, unique within its set of contingent orders</param>
        /// <param name="type">The contingency type</param>
        /// <param name="role">The role of the order in this contingency, required for <see cref="ContingencyType.OneTriggersOther"/> only</param>
        /// <param name="triggered">For a child, whether it was already triggered</param>
        /// <param name="triggeredTime">For a child, the utc time at which it was triggered</param>
        [JsonConstructor]
        public ContingencyLink(int id, ContingencyType type, ContingencyRole? role = null, bool triggered = false, DateTime? triggeredTime = null)
        {
            if (!IsValidRole(type, role))
            {
                throw new ArgumentException($"Invalid contingency role '{role?.ToString() ?? "null"}' for a '{type}' contingency");
            }

            Id = id;
            Type = type;
            Role = role;
            Triggered = triggered;
            TriggeredTime = triggeredTime;
        }

        /// <summary>
        /// Determines whether the role is valid for the contingency type: <see cref="ContingencyType.OneTriggersOther"/> has
        /// a parent and children, while the orders of the other types are all siblings, with no role
        /// </summary>
        public static bool IsValidRole(ContingencyType type, ContingencyRole? role)
        {
            return (type == ContingencyType.OneTriggersOther) == role.HasValue;
        }

        /// <summary>
        /// Creates a copy of this instance
        /// </summary>
        public ContingencyLink Clone()
        {
            return new ContingencyLink(Id, Type, Role, Triggered, TriggeredTime);
        }

        /// <summary>
        /// Returns a string that represents the current object
        /// </summary>
        public override string ToString()
        {
            var role = Role.HasValue ? $":{Role}" : string.Empty;
            var state = Role == ContingencyRole.Child ? (Triggered ? ":Triggered" : ":Held") : string.Empty;
            return $"{Type}:{Id}{role}{state}";
        }
    }
}
