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
using Newtonsoft.Json;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable directory entry for an account discovered through a multi-account brokerage connection.
    /// </summary>
    public class BrokerageAccountDirectoryEntry
    {
        /// <summary>
        /// Gets the brokerage account identifier.
        /// </summary>
        public string AccountId { get; }

        /// <summary>
        /// Gets the account's relationship to the brokerage connection.
        /// </summary>
        public BrokerageAccountRelationship Relationship { get; }

        /// <summary>
        /// Gets every discovered group containing the account.
        /// </summary>
        public IReadOnlyList<string> GroupNames { get; }

        /// <summary>
        /// Gets the brokerage-reported account type when account state was collected and the brokerage supplied it;
        /// otherwise, an empty string.
        /// </summary>
        public string AccountType { get; }

        /// <summary>
        /// Gets the brokerage-provided account-family code, when available.
        /// </summary>
        public string FamilyCode { get; }

        /// <summary>
        /// Gets the brokerage-provided account alias, when available.
        /// </summary>
        public string AccountAlias { get; }

        /// <summary>
        /// Initializes an immutable brokerage account directory entry.
        /// </summary>
        /// <param name="accountId">Brokerage account identifier.</param>
        /// <param name="relationship">Relationship to the brokerage connection.</param>
        /// <param name="groupNames">Discovered groups containing the account.</param>
        /// <param name="accountType">
        /// Brokerage-reported account type, or an empty string when not collected or not supplied.
        /// </param>
        /// <param name="familyCode">Brokerage-provided account-family code.</param>
        /// <param name="accountAlias">Brokerage-provided account alias.</param>
        [JsonConstructor]
        public BrokerageAccountDirectoryEntry(
            string accountId,
            BrokerageAccountRelationship relationship,
            IEnumerable<string> groupNames,
            string accountType = "",
            string familyCode = "",
            string accountAlias = "")
        {
            BrokerageAccountCollection.ValidateIdentifier(accountId, nameof(accountId));
            AccountId = accountId;
            Relationship = relationship;
            GroupNames = BrokerageAccountCollection.CopyIdentifiers(
                groupNames,
                nameof(groupNames));
            AccountType = accountType?.Trim() ?? string.Empty;
            FamilyCode = familyCode?.Trim() ?? string.Empty;
            AccountAlias = accountAlias?.Trim() ?? string.Empty;
        }
    }
}
