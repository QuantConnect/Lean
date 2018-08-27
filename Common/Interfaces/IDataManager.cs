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

using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Subscription handler interface
    /// </summary>
    public interface IDataManager
    {
        /// <summary>
        /// Gets all the current data config subscriptions that are being processed
        /// </summary>
        IEnumerable<SubscriptionDataConfig> SubscriptionManagerSubscriptions { get; }

        /// <summary>
        /// Adds a new subscription data config
        /// </summary>
        /// <returns>True, if the configuration was added successfully</returns>
        bool SubscriptionManagerTryAdd(SubscriptionDataConfig config);

        /// <summary>
        /// Returns true if the given subscription data config is already present
        /// </summary>
        bool SubscriptionManagerContainsKey(SubscriptionDataConfig config);

        /// <summary>
        /// Returns the amount of data config subscriptions processed
        /// </summary>
        int SubscriptionManagerCount();
    }
}
