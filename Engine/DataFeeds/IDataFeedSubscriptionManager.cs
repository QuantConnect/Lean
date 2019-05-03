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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// DataFeedSubscriptionManager interface will manage the subscriptions for the Data Feed
    /// </summary>
    public interface IDataFeedSubscriptionManager
    {
        /// <summary>
        /// Event fired when a new subscription is added
        /// </summary>
        event EventHandler<Subscription> SubscriptionAdded;

        /// <summary>
        /// Event fired when an existing subscription is removed
        /// </summary>
        event EventHandler<Subscription> SubscriptionRemoved;

        /// <summary>
        /// Gets the data feed subscription collection
        /// </summary>
        SubscriptionCollection DataFeedSubscriptions { get; }

        /// <summary>
        /// Get the universe selection instance
        /// </summary>
        UniverseSelection UniverseSelection { get; }

        /// <summary>
        /// Removes the <see cref="Subscription"/>, if it exists
        /// </summary>
        /// <param name="configuration">The <see cref="SubscriptionDataConfig"/> of the subscription to remove</param>
        /// <param name="universe">Universe requesting to remove <see cref="Subscription"/>.
        /// Default value, null, will remove all universes</param>
        /// <returns>True if the subscription was successfully removed, false otherwise</returns>
        bool RemoveSubscription(SubscriptionDataConfig configuration, Universe universe = null);

        /// <summary>
        /// Adds a new <see cref="Subscription"/> to provide data for the specified security.
        /// </summary>
        /// <param name="request">Defines the <see cref="SubscriptionRequest"/> to be added</param>
        /// <returns>True if the subscription was created and added successfully, false otherwise</returns>
        bool AddSubscription(SubscriptionRequest request);
    }
}
