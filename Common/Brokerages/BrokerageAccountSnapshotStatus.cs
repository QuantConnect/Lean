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
    /// State of a brokerage account snapshot.
    /// </summary>
    public enum BrokerageAccountSnapshotStatus
    {
        /// <summary>
        /// No snapshot is available. The brokerage may not expose account-level state, or no request has yet
        /// produced a snapshot.
        /// </summary>
        Unavailable,

        /// <summary>
        /// An accepted snapshot refresh is queued or in progress.
        /// </summary>
        Refreshing,

        /// <summary>
        /// The latest snapshot refresh completed successfully.
        /// </summary>
        Ready,

        /// <summary>
        /// The snapshot is no longer current. Any data from the last successful refresh remains available.
        /// </summary>
        Stale,

        /// <summary>
        /// The latest snapshot refresh failed.
        /// </summary>
        Failed
    }
}
