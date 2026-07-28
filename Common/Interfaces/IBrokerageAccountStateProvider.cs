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
    /// Optional brokerage capability for retrieving account-level state in multi-account structures.
    /// </summary>
    public interface IBrokerageAccountStateProvider
    {
        /// <summary>
        /// Gets the latest immutable account snapshot without performing an external request.
        /// </summary>
        BrokerageAccountSnapshot GetAccountSnapshot();

        /// <summary>
        /// Requests an asynchronous refresh for the requested brokerage account groups and additional managed accounts.
        /// Implementations should coalesce duplicate requests and apply brokerage pacing limits.
        /// </summary>
        /// <param name="groupNames">
        /// Brokerage account groups to refresh. An empty collection requests complete discovery of every group and
        /// account managed by the brokerage connection.
        /// </param>
        /// <param name="additionalAccountIds">Additional managed accounts to refresh outside the selected groups.</param>
        /// <returns>True if the request was accepted or coalesced; otherwise, false.</returns>
        bool RequestAccountSnapshotRefresh(
            IReadOnlyCollection<string> groupNames,
            IReadOnlyCollection<string> additionalAccountIds);

        /// <summary>
        /// Requests a refresh using the provider's existing scope. When no scope has been configured,
        /// the provider selects its deployment-safe default.
        /// </summary>
        /// <returns>True if the request was accepted or coalesced; otherwise, false.</returns>
        bool RequestConfiguredAccountSnapshotRefresh();
    }
}
