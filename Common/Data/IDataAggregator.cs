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

namespace QuantConnect.Data
{
    /// <summary>
    /// Aggregates ticks and bars based on given subscriptions.
    /// </summary>
    public interface IDataAggregator : IDisposable
    {
        /// <summary>
        /// Add new subscription to current <see cref="IDataAggregator"/> instance
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        /// <returns></returns>
        IEnumerator<BaseData> Add(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler);

        /// <summary>
        /// Remove the given subscription
        /// </summary>
        /// <param name="dataConfig">defines the subscription configuration data.</param>        
        /// <returns>Returns true if given <see cref="SubscriptionDataConfig"/> was found and succesfully removed; otherwise false.</returns>
        bool Remove(SubscriptionDataConfig dataConfig);

        /// <summary>
        /// Adds new <see cref="BaseData"/> input into aggregator.
        /// </summary>
        /// <param name="input">The new data</param>
        void Update(BaseData input);
    }
}
