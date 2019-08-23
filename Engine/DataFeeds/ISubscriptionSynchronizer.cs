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
 *
*/

using System;
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides the ability to synchronize subscriptions into time slices
    /// </summary>
    public interface ISubscriptionSynchronizer
    {
        /// <summary>
        /// Event fired when a subscription is finished
        /// </summary>
        event EventHandler<Subscription> SubscriptionFinished;

        /// <summary>
        /// Syncs the specified subscriptions. The frontier time used for synchronization is
        /// managed internally and dependent upon previous synchronization operations.
        /// </summary>
        /// <param name="subscriptions">The subscriptions to sync</param>
        /// <param name="cancellationToken">The cancellation token to stop enumeration</param>
        IEnumerable<TimeSlice> Sync(IEnumerable<Subscription> subscriptions, CancellationToken cancellationToken);
    }
}