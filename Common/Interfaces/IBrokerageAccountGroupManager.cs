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
using QuantConnect.Brokerages;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Optional brokerage capability for asynchronously assigning a managed account to exactly one group or no groups.
    /// </summary>
    public interface IBrokerageAccountGroupManager
    {
        /// <summary>
        /// Gets the latest immutable assignment result without performing an external request.
        /// </summary>
        BrokerageAccountGroupAssignment GetAccountGroupAssignment();

        /// <summary>
        /// Requests an optimistically version-validated account-group assignment. The implementation removes the
        /// account from every current group before optionally assigning it to <paramref name="targetGroupName"/>.
        /// Implementations may reject a request that would leave an existing brokerage group empty.
        /// </summary>
        /// <remarks>
        /// A brokerage that does not provide conditional configuration updates cannot guarantee compare-and-swap
        /// semantics. Such deployments require a single authoritative configuration writer.
        /// </remarks>
        /// <param name="accountId">Managed subaccount to assign.</param>
        /// <param name="targetGroupName">Destination group, or an empty string to leave the account unassigned.</param>
        /// <param name="expectedMembershipHash">
        /// Membership, managed-account, and account-alias hash from the latest ready account snapshot.
        /// </param>
        /// <param name="expectedGroupConfigurationVersion">
        /// Brokerage-wide group configuration version from the latest ready account snapshot.
        /// </param>
        /// <param name="targetAllocationValue">
        /// Optional brokerage-defined per-account allocation value for the destination group. Implementations may
        /// require or reject this value according to the destination group's allocation method.
        /// </param>
        /// <returns>True when the asynchronous request was accepted; otherwise, false.</returns>
        bool RequestAccountGroupAssignment(
            string accountId,
            string targetGroupName,
            string expectedMembershipHash,
            string expectedGroupConfigurationVersion,
            decimal? targetAllocationValue = null);
    }
}
