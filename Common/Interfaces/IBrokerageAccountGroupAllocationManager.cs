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
    /// Optional brokerage capability for replacing an existing account group's complete allocation vector.
    /// </summary>
    public interface IBrokerageAccountGroupAllocationManager
    {
        /// <summary>
        /// Gets the latest immutable allocation update result without performing an external request.
        /// </summary>
        BrokerageAccountGroupAllocationUpdate GetAccountGroupAllocationUpdate();

        /// <summary>
        /// Requests an optimistically version-validated replacement of every per-account allocation value for an
        /// existing group. The allocation keys must exactly match the group's existing members; this operation does
        /// not change membership.
        /// </summary>
        /// <remarks>
        /// A brokerage that does not provide conditional configuration updates cannot guarantee compare-and-swap
        /// semantics. Such deployments require a single authoritative configuration writer.
        /// </remarks>
        /// <param name="groupName">Existing managed account group to update.</param>
        /// <param name="accountAllocationValues">Complete per-account allocation vector.</param>
        /// <param name="expectedMembershipHash">Membership hash from the latest ready account snapshot.</param>
        /// <param name="expectedGroupConfigurationVersion">
        /// Brokerage-wide group configuration version from the latest ready account snapshot.
        /// </param>
        /// <returns>True when the asynchronous request was accepted; otherwise, false.</returns>
        bool RequestAccountGroupAllocationUpdate(
            string groupName,
            IReadOnlyDictionary<string, decimal> accountAllocationValues,
            string expectedMembershipHash,
            string expectedGroupConfigurationVersion);
    }
}
