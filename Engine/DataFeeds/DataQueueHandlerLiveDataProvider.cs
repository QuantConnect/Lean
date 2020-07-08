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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Provides a wrapper around the <see cref="IDataQueueHandler"/> interface
    /// for the new <see cref="ILiveDataProvider"/>.
    /// </summary>
    /// <remarks>
    /// The new <see cref="ILiveDataProvider"/> can provide additional information
    /// to the live data feed, such as Resolution. This method provides a wrapper
    /// around the already existing <see cref="IDataQueueHandler"/> for backwards
    /// compatibility.
    /// </remarks>
    public class DataQueueHandlerLiveDataProvider : ILiveDataProvider
    {
        private readonly LiveNodePacket _jobPacket;

        /// <summary>
        /// IDataQueueHandler instance.
        /// </summary>
        public IDataQueueHandler DataQueueHandler { get; }

        /// <summary>
        /// Creates an instance of the class that wraps around a given <see cref="IDataQueueHandler"/>
        /// </summary>
        /// <param name="queueHandler">DataQueueHandler to wrap</param>
        /// <param name="job">Node packet job</param>
        public DataQueueHandlerLiveDataProvider(IDataQueueHandler queueHandler, LiveNodePacket job)
        {
            DataQueueHandler = queueHandler;
            _jobPacket = job;
        }

        /// <summary>
        /// Gets the next data from the DataQueueHandler
        /// </summary>
        /// <returns>BaseData instances (Tick, TradeBar, QuoteBar, custom, etc.)</returns>
        public IEnumerable<BaseData> Next()
        {
            return DataQueueHandler.GetNextTicks();
        }

        /// <summary>
        /// Provides the subscription config's Symbols to add to the underlying DataQueueHandler.
        /// </summary>
        /// <param name="subscriptions">Subscriptions to add</param>
        public void Subscribe(IEnumerable<SubscriptionDataConfig> subscriptions)
        {
            DataQueueHandler.Subscribe(
                _jobPacket,
                subscriptions.Select(x => x.Symbol).ToHashSet());
        }

        /// <summary>
        /// Provides the subscription config's Symbols to remove from the underlying DataQueueHandler.
        /// </summary>
        /// <param name="subscriptions">Subscriptions to remove</param>
        public void Unsubscribe(IEnumerable<SubscriptionDataConfig> subscriptions)
        {
            DataQueueHandler.Unsubscribe(
                _jobPacket,
                subscriptions.Select(x => x.Symbol).ToHashSet());
        }
    }
}
