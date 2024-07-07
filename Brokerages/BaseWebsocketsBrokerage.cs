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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides shared brokerage websockets implementation
    /// </summary>
    public abstract class BaseWebsocketsBrokerage : Brokerage
    {
        private const int ConnectionTimeout = 30000;

        /// <summary>
        /// True if the current brokerage is already initialized
        /// </summary>
        protected bool IsInitialized { get; set; }

        /// <summary>
        /// The websockets client instance
        /// </summary>
        protected IWebSocket WebSocket { get; set; }

        /// <summary>
        /// The rest client instance
        /// </summary>
        protected IRestClient RestClient { get; set; }

        /// <summary>
        /// standard json parsing settings
        /// </summary>
        protected JsonSerializerSettings JsonSettings { get; set; }

        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public ConcurrentDictionary<int, Orders.Order> CachedOrderIDs { get; set; }

        /// <summary>
        /// The api secret
        /// </summary>
        protected string ApiSecret { get; set; }

        /// <summary>
        /// The api key
        /// </summary>
        protected string ApiKey { get; set; }

        /// <summary>
        /// Count subscribers for each (symbol, tickType) combination
        /// </summary>
        protected DataQueueHandlerSubscriptionManager SubscriptionManager { get; set; }

        /// <summary>
        /// Initialize the instance of this class
        /// </summary>
        /// <param name="wssUrl">The web socket base url</param>
        /// <param name="websocket">instance of websockets client</param>
        /// <param name="restClient">instance of rest client</param>
        /// <param name="apiKey">api key</param>
        /// <param name="apiSecret">api secret</param>
        protected void Initialize(
            string wssUrl,
            IWebSocket websocket,
            IRestClient restClient,
            string apiKey,
            string apiSecret
        )
        {
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;
            JsonSettings = new JsonSerializerSettings
            {
                FloatParseHandling = FloatParseHandling.Decimal
            };
            CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();

            WebSocket = websocket;
            WebSocket.Initialize(wssUrl);
            WebSocket.Message += OnMessage;

            WebSocket.Open += (sender, args) =>
            {
                Log.Trace($"BaseWebsocketsBrokerage(): WebSocket.Open. Subscribing");
                Subscribe(GetSubscribed());
            };

            RestClient = restClient;
            ApiSecret = apiSecret;
            ApiKey = apiKey;
        }

        /// <summary>
        /// Creates an instance of a websockets brokerage
        /// </summary>
        /// <param name="name">Name of brokerage</param>
        protected BaseWebsocketsBrokerage(string name)
            : base(name) { }

        /// <summary>
        /// Handles websocket received messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected abstract void OnMessage(object sender, WebSocketMessage e);

        /// <summary>
        /// Creates wss connection, monitors for disconnection and re-connects when necessary
        /// </summary>
        public override void Connect()
        {
            if (IsConnected)
                return;

            Log.Trace("BaseWebSocketsBrokerage.Connect(): Connecting...");

            ConnectSync();
        }

        /// <summary>
        /// Handles the creation of websocket subscriptions
        /// </summary>
        /// <param name="symbols"></param>
        protected abstract bool Subscribe(IEnumerable<Symbol> symbols);

        /// <summary>
        /// Gets a list of current subscriptions
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Symbol> GetSubscribed()
        {
            return SubscriptionManager?.GetSubscribedSymbols() ?? Enumerable.Empty<Symbol>();
        }

        /// <summary>
        /// Start websocket connect
        /// </summary>
        protected void ConnectSync()
        {
            var resetEvent = new ManualResetEvent(false);
            EventHandler triggerEvent = (o, args) => resetEvent.Set();
            WebSocket.Open += triggerEvent;

            WebSocket.Connect();

            if (!resetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException("Websockets connection timeout.");
            }
            WebSocket.Open -= triggerEvent;
        }
    }
}
