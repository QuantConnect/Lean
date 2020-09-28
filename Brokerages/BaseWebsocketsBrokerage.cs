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
using QuantConnect.Data;
using QuantConnect.Logging;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Provides shared brokerage websockets implementation
    /// </summary>
    public abstract class BaseWebsocketsBrokerage : Brokerage
    {
        private const int ConnectionTimeout = 30000;

        /// <summary>
        /// The websockets client instance
        /// </summary>
        protected readonly IWebSocket WebSocket;

        /// <summary>
        /// The rest client instance
        /// </summary>
        protected readonly IRestClient RestClient;

        /// <summary>
        /// standard json parsing settings
        /// </summary>
        protected readonly JsonSerializerSettings JsonSettings;

        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public readonly ConcurrentDictionary<int, Orders.Order> CachedOrderIDs;

        /// <summary>
        /// The api secret
        /// </summary>
        protected readonly string ApiSecret;

        /// <summary>
        /// The api key
        /// </summary>
        protected readonly string ApiKey;

        /// <summary>
        /// Count subscribers for each (symbol, tickType) combination
        /// </summary>
        protected DataQueueHandlerSubscriptionManager SubscriptionManager;

        /// <summary>
        /// Creates an instance of a websockets brokerage
        /// </summary>
        /// <param name="wssUrl">Websockets base url</param>
        /// <param name="websocket">Websocket client instance</param>
        /// <param name="restClient">Rest client instance</param>
        /// <param name="apiKey">Brokerage api auth key</param>
        /// <param name="apiSecret">Brokerage api auth secret</param>
        /// <param name="name">Name of brokerage</param>
        protected BaseWebsocketsBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string name) : base(name)
        {
            JsonSettings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };
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
        /// Handles websocket received messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public abstract void OnMessage(object sender, WebSocketMessage e);

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
        public abstract void Subscribe(IEnumerable<Symbol> symbols);

        /// <summary>
        /// Gets a list of current subscriptions
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Symbol> GetSubscribed()
        {
            return SubscriptionManager?.GetSubscribedSymbols() ?? Enumerable.Empty<Symbol>();
        }

        private void ConnectSync()
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
