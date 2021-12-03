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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used to handle multiple live datafeeds
    /// </summary>
    public class CompositeDataQueueHandler : IDataQueueHandler
    {
        private readonly List<IDataQueueHandler> _dataHandlers = new();
        private readonly Dictionary<SubscriptionDataConfig, IDataQueueHandler> _dataConfigAndDataHandler = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeDataQueueHandler"/> class
        /// </summary>
        public CompositeDataQueueHandler()
        {
        }

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            foreach (var dataHandler in _dataHandlers)
            {
                var enumerator = dataHandler.Subscribe(dataConfig, newDataAvailableHandler);
                // Check if the enumerator is not empty
                if (enumerator != null)
                {
                    _dataConfigAndDataHandler.Add(dataConfig, dataHandler);
                    return enumerator;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public virtual void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _dataConfigAndDataHandler.TryGetValue(dataConfig, out IDataQueueHandler dataHandler);
            dataHandler?.Unsubscribe(dataConfig);
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
            var dataHandlersConfig = job.DataQueueHandler;
            Log.Trace($"CompositeDataQueueHandler.SetJob(): will use {dataHandlersConfig}");
            foreach (var dataHandlerName in dataHandlersConfig.DeserializeList())
            {
                var dataHandler = Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(dataHandlerName);
                dataHandler.SetJob(job);
                _dataHandlers.Add(dataHandler);
            }
        }

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>true if the data provider is connected</returns>
        public bool IsConnected => true;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var dataHandler in _dataHandlers)
            {
                dataHandler.Dispose();
            }
        }
    }
}
