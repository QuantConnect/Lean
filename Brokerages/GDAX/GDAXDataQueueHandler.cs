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
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;
using RestSharp;

namespace QuantConnect.Brokerages.GDAX
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for GDAX
    /// </summary>
    [BrokerageFactory(typeof(GDAXBrokerageFactory))]
    public class GDAXDataQueueHandler : GDAXBrokerage, IDataQueueHandler
    {
        private bool _isInitialized;

        /// <summary>
        /// Constructor for brokerage
        /// </summary>
        public GDAXDataQueueHandler() : base("GDAX")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GDAXDataQueueHandler"/> class
        /// </summary>
        public GDAXDataQueueHandler(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm,
            IPriceProvider priceProvider, IDataAggregator aggregator, LiveNodePacket job)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, passPhrase, algorithm, priceProvider, aggregator, job)
        {
            Initialize(
                wssUrl: wssUrl,
                restApiUrl: null,
                websocket: websocket,
                restClient: restClient,
                apiKey: apiKey,
                apiSecret: apiSecret,
                accountId: null,
                accessToken: null,
                passPhrase: passPhrase,
                useSandbox: false,
                algorithm: algorithm,
                orderProvider: null,
                securityProvider: null,
                priceProvider: priceProvider,
                aggregator: aggregator,
                job: job
            );
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
                return null;
            }

            if (!WebSocket.IsOpen)
            {
                Connect();
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
            var wssUrl = job.BrokerageData["gdax-url"];
            var restApi = job.BrokerageData["gdax-rest-api"];
            var restClient = new RestClient(restApi);
            var webSocketClient = new WebSocketClientWrapper();
            var passPhrase = job.BrokerageData["gdax-passphrase"];
            var apiKey = job.BrokerageData["gdax-api-key"];
            var apiSecret = job.BrokerageData["gdax-api-secret"];
            var priceProvider = new ApiPriceProvider(job.UserId, job.UserToken);
            var aggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
                Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));

            Initialize(
                wssUrl: wssUrl,
                restApiUrl: null,
                websocket: webSocketClient,
                restClient: restClient,
                apiKey: apiKey,
                apiSecret: apiSecret,
                accountId: null,
                accessToken: null,
                passPhrase: passPhrase,
                useSandbox: false,
                algorithm: null,
                orderProvider: null,
                securityProvider: null,
                priceProvider: priceProvider,
                aggregator: aggregator,
                job: job
            );
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
        /// Initialize the instance of this class
        /// </summary>
        /// <param name="wssUrl">The web socket base url</param>
        /// <param name="restApiUrl">The rest api url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        /// <param name="accountId">account id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="passPhrase">pass phrase</param>
        /// <param name="useSandbox">use sandbox</param>
        /// <param name="algorithm">the algorithm instance is required to retrieve account type</param>
        /// <param name="orderProvider">order provider instance</param>
        /// <param name="securityProvider">security provider instance</param>
        /// <param name="priceProvider">The price provider for missing FX conversion rates</param>
        /// <param name="aggregator">the aggregator for consolidating ticks</param>
        /// <param name="job">The live job packet</param>
        protected override void Initialize(string wssUrl, string restApiUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret,
            string accountId, string accessToken, string passPhrase, bool useSandbox, IAlgorithm algorithm, IOrderProvider orderProvider,
            ISecurityProvider securityProvider, IPriceProvider priceProvider, IDataAggregator aggregator, LiveNodePacket job)
        {
            if (!_isInitialized)
            {
                base.Initialize(
                    wssUrl: wssUrl,
                    restApiUrl: restApiUrl,
                    websocket: websocket,
                    restClient: restClient,
                    apiKey: apiKey,
                    apiSecret: apiSecret,
                    accountId: accountId,
                    accessToken: accessToken,
                    passPhrase: passPhrase,
                    useSandbox: useSandbox,
                    algorithm: algorithm,
                    orderProvider: orderProvider,
                    securityProvider: securityProvider,
                    priceProvider: priceProvider,
                    aggregator: aggregator,
                    job: job
                );
                var subscriptionManager = new EventBasedDataQueueHandlerSubscriptionManager();
                subscriptionManager.SubscribeImpl += (s, t) =>
                {
                    Subscribe(s);
                    return true;
                };
                subscriptionManager.UnsubscribeImpl += (s, t) => Unsubscribe(s);

                SubscriptionManager = subscriptionManager;
                _isInitialized = true;
            }
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
