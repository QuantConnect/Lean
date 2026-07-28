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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Immutable result of the latest brokerage account-group assignment.
    /// A blank target group means the account should be unassigned from every group.
    /// </summary>
    public class BrokerageAccountGroupAssignment
    {
        /// <summary>
        /// Result returned when the brokerage does not expose account-group management.
        /// </summary>
        public static BrokerageAccountGroupAssignment Unavailable { get; } = new(
            BrokerageAccountGroupAssignmentStatus.Unavailable,
            0,
            default,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>(),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            null);

        /// <summary>
        /// Gets the current assignment status.
        /// </summary>
        public BrokerageAccountGroupAssignmentStatus Status { get; }

        /// <summary>
        /// Gets the monotonically increasing assignment generation.
        /// </summary>
        public long Generation { get; }

        /// <summary>
        /// Gets the UTC time at which this result was published.
        /// </summary>
        public DateTime AsOfUtc { get; }

        /// <summary>
        /// Gets the managed subaccount being assigned.
        /// </summary>
        public string AccountId { get; }

        /// <summary>
        /// Gets the requested destination group, or an empty string when removing every assignment.
        /// </summary>
        public string TargetGroupName { get; }

        /// <summary>
        /// Gets the requested per-account allocation value for a user-specified destination group.
        /// </summary>
        public decimal? TargetAllocationValue { get; }

        /// <summary>
        /// Gets every group containing the account before the operation.
        /// </summary>
        public IReadOnlyList<string> PreviousGroupNames { get; }

        /// <summary>
        /// Gets every group containing the account after confirmed readback.
        /// </summary>
        public IReadOnlyList<string> ResultingGroupNames { get; }

        /// <summary>
        /// Gets the membership hash supplied by the requesting snapshot.
        /// </summary>
        public string ExpectedMembershipHash { get; }

        /// <summary>
        /// Gets the membership hash after confirmed readback and snapshot refresh.
        /// </summary>
        public string ResultingMembershipHash { get; }

        /// <summary>
        /// Gets the brokerage-wide group configuration version supplied by the requesting snapshot.
        /// </summary>
        public string ExpectedGroupConfigurationVersion { get; }

        /// <summary>
        /// Gets the brokerage-wide group configuration version after confirmed readback.
        /// </summary>
        public string ResultingGroupConfigurationVersion { get; }

        /// <summary>
        /// Gets the failure reason, or an empty string when no failure occurred.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets whether the assignment is still being processed.
        /// </summary>
        public bool IsPending => Status == BrokerageAccountGroupAssignmentStatus.Pending;

        /// <summary>
        /// Gets whether the immutable assignment result is in a terminal state.
        /// </summary>
        public bool IsCompleted => Status != BrokerageAccountGroupAssignmentStatus.Pending;

        /// <summary>
        /// Initializes an immutable account-group assignment result.
        /// </summary>
        /// <param name="status">Assignment status.</param>
        /// <param name="generation">Monotonically increasing assignment generation.</param>
        /// <param name="asOfUtc">UTC time at which this result was published.</param>
        /// <param name="accountId">Managed subaccount being assigned.</param>
        /// <param name="targetGroupName">Requested destination group, or an empty string for no group.</param>
        /// <param name="previousGroupNames">Groups containing the account before the operation.</param>
        /// <param name="resultingGroupNames">Groups containing the account after confirmed readback.</param>
        /// <param name="expectedMembershipHash">Membership hash supplied by the requesting snapshot.</param>
        /// <param name="resultingMembershipHash">Membership hash after confirmed readback.</param>
        /// <param name="expectedGroupConfigurationVersion">
        /// Group configuration version supplied by the requesting snapshot.
        /// </param>
        /// <param name="resultingGroupConfigurationVersion">Group configuration version after confirmed readback.</param>
        /// <param name="errorMessage">Failure reason, or an empty string when no failure occurred.</param>
        /// <param name="targetAllocationValue">Optional brokerage-defined destination allocation value.</param>
        public BrokerageAccountGroupAssignment(
            BrokerageAccountGroupAssignmentStatus status,
            long generation,
            DateTime asOfUtc,
            string accountId,
            string targetGroupName,
            IEnumerable<string> previousGroupNames,
            IEnumerable<string> resultingGroupNames,
            string expectedMembershipHash,
            string resultingMembershipHash,
            string expectedGroupConfigurationVersion,
            string resultingGroupConfigurationVersion,
            string errorMessage,
            decimal? targetAllocationValue = null)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(generation),
                    "The assignment generation cannot be negative.");
            }
            if (status != BrokerageAccountGroupAssignmentStatus.Unavailable)
            {
                BrokerageAccountCollection.ValidateIdentifier(
                    accountId,
                    nameof(accountId));
                if (!string.IsNullOrEmpty(targetGroupName))
                {
                    BrokerageAccountCollection.ValidateIdentifier(
                        targetGroupName,
                        nameof(targetGroupName));
                }
            }
            Status = status;
            Generation = generation;
            AsOfUtc = asOfUtc;
            AccountId = accountId ?? string.Empty;
            TargetGroupName = targetGroupName ?? string.Empty;
            TargetAllocationValue = targetAllocationValue;
            PreviousGroupNames = CopyGroupNames(
                previousGroupNames,
                nameof(previousGroupNames));
            ResultingGroupNames = CopyGroupNames(
                resultingGroupNames,
                nameof(resultingGroupNames));
            ExpectedMembershipHash = expectedMembershipHash ?? string.Empty;
            ResultingMembershipHash = resultingMembershipHash ?? string.Empty;
            ExpectedGroupConfigurationVersion = expectedGroupConfigurationVersion ?? string.Empty;
            ResultingGroupConfigurationVersion = resultingGroupConfigurationVersion ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        private static IReadOnlyList<string> CopyGroupNames(
            IEnumerable<string> groupNames,
            string parameterName)
        {
            return BrokerageAccountCollection.CopyIdentifiers(
                groupNames,
                parameterName);
        }
    }
}
