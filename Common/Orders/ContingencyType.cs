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
    /// The type of relationship linking a set of contingent orders
    /// </summary>
    public enum ContingencyType
    {
        /// <summary>
        /// One Cancels Other (OCO/OCA): once a member fills the remaining members are canceled (0)
        /// </summary>
        OneCancelsOther,

        /// <summary>
        /// One Triggers Other (OTO): the children are held until the parent is completely filled (1)
        /// </summary>
        OneTriggersOther,

        /// <summary>
        /// One Updates Other (OUO): a member fill reduces the quantity of the remaining members proportionally,
        /// which are canceled once the member is completely filled (2)
        /// </summary>
        OneUpdatesOther
    }

    /// <summary>
    /// The role an order plays in a <see cref="ContingencyType.OneTriggersOther"/> contingency, the only one with sides.
    /// The orders of a <see cref="ContingencyType.OneCancelsOther"/> or <see cref="ContingencyType.OneUpdatesOther"/> contingency
    /// are all siblings, they have no role
    /// </summary>
    public enum ContingencyRole
    {
        /// <summary>
        /// The parent, which triggers the children once completely filled (0)
        /// </summary>
        Parent,

        /// <summary>
        /// A child, held until its parent fills (1)
        /// </summary>
        Child
    }
}
