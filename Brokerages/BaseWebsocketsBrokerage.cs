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
using System.Threading;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Provides shared brokerage websockets implementation
    /// </summary>
    public abstract class BaseWebsocketsBrokerage : Brokerage
    {
        private const int ConnectionTimeout = 30000;
        private readonly IConnectionHandler _connectionHandler;

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
        /// Timestamp of most recent heartbeat message
        /// </summary>
        protected DateTime LastHeartbeatUtcTime;

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
        /// <param name="websocketMaximumIdle">The maximum amount of time the socket can go idle before triggering a reconnect</param>
        /// <param name="name">Name of brokerage</param>
        protected BaseWebsocketsBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, TimeSpan websocketMaximumIdle, string name) : base(name)
        {
            JsonSettings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };
            CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();
            _connectionHandler = new DefaultConnectionHandler { MaximumIdleTimeSpan = websocketMaximumIdle };

            WebSocket = websocket;
            WebSocket.Initialize(wssUrl);
            WebSocket.Message += (sender, message) =>
            {
                OnMessage(sender, message);
                _connectionHandler.KeepAlive(LastHeartbeatUtcTime);
            };

            _connectionHandler.Initialize(Guid.NewGuid().ToString());
            _connectionHandler.ReconnectRequested += (sender, args) =>
            {
                Reconnect();
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
            _connectionHandler.EnableMonitoring(true);
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            _connectionHandler?.EnableMonitoring(false);
        }

        /// <summary>
        /// Handles the creation of websocket subscriptions
        /// </summary>
        /// <param name="symbols"></param>
        public abstract void Subscribe(IEnumerable<Symbol> symbols);

        /// <summary>
        /// Dispose of the connection handler
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _connectionHandler?.DisposeSafely();
        }

        /// <summary>
        /// Gets a list of current subscriptions
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Symbol> GetSubscribed()
        {
            return SubscriptionManager.GetSubscribedSymbols();
        }

        /// <summary>
        /// Handles reconnections in the event of connection loss
        /// </summary>
        private void Reconnect()
        {
            Log.Trace($"BaseWebsocketsBrokerage(): Reconnecting... IsConnected: {IsConnected}");
            var subscribed = GetSubscribed();

            try
            {
                //try to clean up state
                if (IsConnected)
                {
                    CloseSync();
                }
                if (!IsConnected)
                {
                    ConnectSync();
                }
            }
            finally
            {
                if (IsConnected)
                {
                    Subscribe(subscribed);
                }
            }
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

        private void CloseSync()
        {
            var resetEvent = new ManualResetEvent(false);
            EventHandler<WebSocketCloseData> triggerEvent = (o, args) => resetEvent.Set();
            WebSocket.Closed += triggerEvent;

            WebSocket.Close();

            if (!resetEvent.WaitOne(ConnectionTimeout))
            {
                throw new TimeoutException("Websocket close timeout.");
            }
            WebSocket.Closed -= triggerEvent;
        }
    }
}
