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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using RestSharp;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for GDAX
    /// </summary>
    [BrokerageFactory(typeof(GDAXBrokerageFactory))]
    public class GDAXDataQueueHandler : GDAXBrokerage, IDataQueueHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GDAXDataQueueHandler"/> class
        /// </summary>
        public GDAXDataQueueHandler(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm,
            IPriceProvider priceProvider, IDataAggregator aggregator)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, passPhrase, algorithm, priceProvider, aggregator)
        {
            var subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
            subscriptionManager.SubscribeImpl += (s,t) =>
            {
                Subscribe(s);
                return true;
            };
            subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

            SubscriptionManager = subscriptionManager;
        }

        /// <summary>
        /// The list of websocket channels to subscribe
        /// </summary>
        protected override string[] ChannelNames { get; } = { "heartbeat", "level2", "matches" };

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _aggregator.Add(dataConfig, newDataAvailableHandler);
            SubscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            SubscriptionManager.Unsubscribe(dataConfig);
            _aggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Checks if this brokerage supports the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>returns true if brokerage supports the specified symbol; otherwise false</returns>
        private static bool CanSubscribe(Symbol symbol)
        {
            if (symbol.Value.Contains("UNIVERSE") ||
                symbol.SecurityType != SecurityType.Forex && symbol.SecurityType != SecurityType.Crypto)
            {
                return false;
            }

            return true;
        }
    }
}
