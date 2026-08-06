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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable brokerage account group and its brokerage-reported members.
    /// </summary>
    public class BrokerageAccountGroup
    {
        /// <summary>
        /// Gets the brokerage account group name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the brokerage-defined allocation method.
        /// </summary>
        public string AllocationMethod { get; }

        /// <summary>
        /// Gets the brokerage-reported account group members.
        /// </summary>
        public IReadOnlyList<string> AccountIds { get; }

        /// <summary>
        /// Gets brokerage-defined per-account allocation values supplied for user-specified allocation methods. The
        /// collection may be empty when the brokerage computes allocations.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> AccountAllocationValues { get; }

        /// <summary>
        /// Initializes an immutable brokerage account group.
        /// </summary>
        /// <param name="name">Brokerage account group name.</param>
        /// <param name="allocationMethod">Brokerage-defined allocation method.</param>
        /// <param name="accountIds">Brokerage-reported account group members.</param>
        /// <param name="accountAllocationValues">Optional per-account allocation values.</param>
        public BrokerageAccountGroup(
            string name,
            string allocationMethod,
            IEnumerable<string> accountIds,
            IReadOnlyDictionary<string, decimal> accountAllocationValues = null)
        {
            BrokerageAccountCollection.ValidateIdentifier(name, nameof(name));
            BrokerageAccountCollection.ValidateIdentifier(
                allocationMethod,
                nameof(allocationMethod));
            Name = name;
            AllocationMethod = allocationMethod;
            AccountIds = BrokerageAccountCollection.CopyIdentifiers(
                accountIds,
                nameof(accountIds));
            AccountAllocationValues = BrokerageAccountCollection.CopyDictionary(
                accountAllocationValues,
                nameof(accountAllocationValues));

            var members = AccountIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unknownAccount = AccountAllocationValues.Keys.FirstOrDefault(accountId => !members.Contains(accountId));
            if (unknownAccount != null)
            {
                throw new ArgumentException(
                    $"Allocation account '{unknownAccount}' is not a member of brokerage account group '{Name}'.",
                    nameof(accountAllocationValues));
            }
        }
    }
}
