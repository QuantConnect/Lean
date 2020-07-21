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
using System.ComponentModel.Composition;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Task requestor interface with cloud system
    /// </summary>
    [InheritedExport(typeof(IDataQueueHandler))]
    public interface IDataQueueHandler : IDisposable
    {
        /// <summary>
        /// Subscribe to the specified symbols
        /// </summary>
        /// <param name="subscriptionRequest">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        IEnumerator<BaseData> Subscribe(SubscriptionRequest subscriptionRequest, EventHandler newDataAvailableHandler);

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        void Unsubscribe(SubscriptionDataConfig dataConfig);

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>True if the data provider is connected</returns>
        bool IsConnected { get; }
    }
}
