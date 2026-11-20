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
    /// Immutable result of the latest brokerage account-group allocation update.
    /// </summary>
    public class BrokerageAccountGroupAllocationUpdate
    {
        /// <summary>
        /// Result returned when no allocation update result is available. The brokerage may not support this
        /// capability, or no request has yet produced a result.
        /// </summary>
        public static BrokerageAccountGroupAllocationUpdate Unavailable { get; } = new(
            BrokerageAccountGroupAllocationUpdateStatus.Unavailable,
            0,
            default,
            string.Empty,
            string.Empty,
            null,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);

        /// <summary>
        /// Gets the current update status.
        /// </summary>
        public BrokerageAccountGroupAllocationUpdateStatus Status { get; }

        /// <summary>
        /// Gets the monotonically increasing update generation.
        /// </summary>
        public long Generation { get; }

        /// <summary>
        /// Gets the UTC time at which this result was published.
        /// </summary>
        public DateTime AsOfUtc { get; }

        /// <summary>
        /// Gets the existing account group being updated.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Gets the allocation method observed when the request was admitted and, after a successful update,
        /// confirmed by brokerage readback.
        /// </summary>
        public string AllocationMethod { get; }

        /// <summary>
        /// Gets the requested complete per-account allocation vector.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> RequestedAccountAllocationValues { get; }

        /// <summary>
        /// Gets the complete allocation vector confirmed by brokerage readback. This value is meaningful only when
        /// <see cref="Status"/> is <see cref="BrokerageAccountGroupAllocationUpdateStatus.Succeeded"/>.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> ResultingAccountAllocationValues { get; }

        /// <summary>
        /// Gets the membership hash supplied by the requesting snapshot.
        /// </summary>
        public string ExpectedMembershipHash { get; }

        /// <summary>
        /// Gets the membership hash after confirmed readback and snapshot refresh. This value is meaningful only
        /// when <see cref="Status"/> is <see cref="BrokerageAccountGroupAllocationUpdateStatus.Succeeded"/>.
        /// </summary>
        public string ResultingMembershipHash { get; }

        /// <summary>
        /// Gets the brokerage-wide group configuration version supplied by the requesting snapshot.
        /// </summary>
        public string ExpectedGroupConfigurationVersion { get; }

        /// <summary>
        /// Gets the brokerage-wide group configuration version after confirmed readback. This value is meaningful
        /// only when <see cref="Status"/> is <see cref="BrokerageAccountGroupAllocationUpdateStatus.Succeeded"/>.
        /// </summary>
        public string ResultingGroupConfigurationVersion { get; }

        /// <summary>
        /// Gets a provider diagnostic, warning, or failure message, or an empty string when none is available.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets whether this result has a pending status.
        /// </summary>
        public bool IsPending => Status == BrokerageAccountGroupAllocationUpdateStatus.Pending;

        /// <summary>
        /// Gets whether this result does not have a pending status.
        /// </summary>
        public bool IsCompleted => Status != BrokerageAccountGroupAllocationUpdateStatus.Pending;

        /// <summary>
        /// Initializes an immutable account-group allocation update result.
        /// </summary>
        /// <param name="status">Allocation update status.</param>
        /// <param name="generation">Monotonically increasing allocation update generation.</param>
        /// <param name="asOfUtc">UTC time at which this result was published.</param>
        /// <param name="groupName">Existing account group being updated.</param>
        /// <param name="allocationMethod">
        /// Brokerage allocation method observed when the request was admitted or confirmed after a successful update.
        /// </param>
        /// <param name="requestedAccountAllocationValues">Requested complete per-account allocation vector.</param>
        /// <param name="resultingAccountAllocationValues">
        /// Allocation vector confirmed by brokerage readback; meaningful only for a succeeded result.
        /// </param>
        /// <param name="expectedMembershipHash">Membership hash supplied by the requesting snapshot.</param>
        /// <param name="resultingMembershipHash">
        /// Membership hash after confirmed readback; meaningful only for a succeeded result.
        /// </param>
        /// <param name="expectedGroupConfigurationVersion">
        /// Group configuration version supplied by the requesting snapshot.
        /// </param>
        /// <param name="resultingGroupConfigurationVersion">
        /// Group configuration version after confirmed readback; meaningful only for a succeeded result.
        /// </param>
        /// <param name="errorMessage">Provider diagnostic, warning, or failure message.</param>
        public BrokerageAccountGroupAllocationUpdate(
            BrokerageAccountGroupAllocationUpdateStatus status,
            long generation,
            DateTime asOfUtc,
            string groupName,
            string allocationMethod,
            IReadOnlyDictionary<string, decimal> requestedAccountAllocationValues,
            IReadOnlyDictionary<string, decimal> resultingAccountAllocationValues,
            string expectedMembershipHash,
            string resultingMembershipHash,
            string expectedGroupConfigurationVersion,
            string resultingGroupConfigurationVersion,
            string errorMessage)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(generation),
                    "The allocation update generation cannot be negative.");
            }
            if (status != BrokerageAccountGroupAllocationUpdateStatus.Unavailable)
            {
                BrokerageAccountCollection.ValidateIdentifier(
                    groupName,
                    nameof(groupName));
            }
            Status = status;
            Generation = generation;
            AsOfUtc = asOfUtc;
            GroupName = groupName ?? string.Empty;
            AllocationMethod = allocationMethod?.Trim() ?? string.Empty;
            RequestedAccountAllocationValues = CopyAllocations(
                requestedAccountAllocationValues,
                nameof(requestedAccountAllocationValues));
            ResultingAccountAllocationValues = CopyAllocations(
                resultingAccountAllocationValues,
                nameof(resultingAccountAllocationValues));
            ExpectedMembershipHash = expectedMembershipHash ?? string.Empty;
            ResultingMembershipHash = resultingMembershipHash ?? string.Empty;
            ExpectedGroupConfigurationVersion = expectedGroupConfigurationVersion ?? string.Empty;
            ResultingGroupConfigurationVersion = resultingGroupConfigurationVersion ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        private static IReadOnlyDictionary<string, decimal> CopyAllocations(
            IReadOnlyDictionary<string, decimal> allocations,
            string parameterName)
        {
            return BrokerageAccountCollection.CopyDictionary(
                allocations,
                parameterName);
        }
    }
}
