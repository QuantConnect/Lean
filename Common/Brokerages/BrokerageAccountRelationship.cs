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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Relationship of an account identifier to the brokerage connection that produced the snapshot.
    /// This describes API topology only and does not assert beneficial ownership or legal client status.
    /// </summary>
    public enum BrokerageAccountRelationship
    {
        /// <summary>
        /// The brokerage could not establish the relationship.
        /// </summary>
        Unknown,

        /// <summary>
        /// The primary account configured for the brokerage connection.
        /// </summary>
        Primary,

        /// <summary>
        /// An aggregate reporting identifier associated with the primary account.
        /// </summary>
        Aggregate,

        /// <summary>
        /// An account reported by the brokerage as managed by the primary connection.
        /// </summary>
        Managed
    }
}
