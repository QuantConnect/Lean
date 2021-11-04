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

using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// This data provider will wrap and use multiple data providers internally in the provided order
    /// </summary>
    public class CompositeDataQueueHandler : IDataQueueHandler
    {
        private readonly List<IDataQueueHandler> _dataHandlers;

        /// <summary>
        /// Creates a new instance and initialize data providers used
        /// </summary>
        public CompositeDataQueueHandler()
        {
            _dataHandlers = new List<IDataQueueHandler>();

            var dataProvidersConfig = Config.Get("composite-data-providers");
            if (!string.IsNullOrEmpty(dataProvidersConfig))
            {
                var dataProviders = JsonConvert.DeserializeObject<List<string>>(dataProvidersConfig);
                foreach (var dataProvider in dataProviders)
                {
                    _dataHandlers.Add(Composer.Instance.GetExportedValueByTypeName<IDataQueueHandler>(dataProvider));
                }

                if (_dataHandlers.Count == 0)
                {
                    throw new ArgumentException("CompositeDataProvider(): requires at least 1 valid data provider in 'composite-data-providers'");
                }
            }
            else
            {
                throw new ArgumentException("CompositeDataProvider(): requires 'composite-data-providers' to be set with a valid type name");
            }
        }

        /// <summary>
        /// Desktop/Local doesn't support live data from this handler
        /// </summary>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            for (var i = 0; i < _dataHandlers.Count; i++)
            {
                var enumerator = _dataHandlers[i].Subscribe(dataConfig, newDataAvailableHandler);

                if (enumerator != null)
                {
                    return enumerator;
                }
            }

            return null;
        }

        /// <summary>
        /// Desktop/Local doesn't support live data from this handler
        /// </summary>
        public virtual void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            throw new NotImplementedException("QuantConnect.Queues.LiveDataQueue has not implemented live data.");
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>true if the data provider is connected</returns>
        public bool IsConnected => false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
