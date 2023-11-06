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

using System.ComponentModel.Composition;
using System;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Monitors data requests and reports on missing data
    /// </summary>
    [InheritedExport(typeof(IDataMonitor))]
    public interface IDataMonitor : IDisposable
    {
        /// <summary>
        /// Terminates the data monitor generating a final report
        /// </summary>
        void Exit();

        /// <summary>
        /// Sets the <see cref="SubscriptionManager"/> to be used by the data monitor
        /// </summary>
        /// <param name="subscriptionManager">The subscription manager to set</param>
        void SetSubscriptionManager(SubscriptionManager subscriptionManager);

        /// <summary>
        /// Event handler for the <see cref="IDataProvider.NewDataRequest"/> event
        /// </summary>
        void OnNewDataRequest(object sender, DataProviderNewDataRequestEventArgs e);

        /// <summary>
        /// Event handler for new order events
        /// </summary>
        void OnOrderEvent(object sender, OrderEvent e);
    }
}
