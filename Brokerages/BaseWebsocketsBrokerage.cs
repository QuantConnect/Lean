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
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Provides shared brokerage websockets implementation
    /// </summary>
    public abstract class BaseWebsocketsBrokerage : Brokerage
    {

        #region Declarations
        /// <summary>
        /// The list of queued ticks
        /// </summary>
        public List<Tick> Ticks = new List<Tick>();
        /// <summary>
        /// The websockets client instance
        /// </summary>
        protected IWebSocket WebSocket;
        /// <summary>
        /// The rest client instance
        /// </summary>
        protected IRestClient RestClient;
        /// <summary>
        /// standard json parsing settings
        /// </summary>
        protected JsonSerializerSettings JsonSettings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };
        /// <summary>
        /// A list of currently active orders
        /// </summary>
        public ConcurrentDictionary<int, Orders.Order> CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();
        /// <summary>
        /// A list of currently subscribed channels
        /// </summary>
        protected Dictionary<string, Channel> ChannelList = new Dictionary<string, Channel>();
        private string _market { get; set; }
        /// <summary>
        /// The api secret
        /// </summary>
        protected string ApiSecret;
        /// <summary>
        /// The api key
        /// </summary>
        protected string ApiKey;
        /// <summary>
        /// Timestamp of most recent heartbeat message
        /// </summary>
        protected DateTime LastHeartbeatUtcTime = DateTime.UtcNow;
        private const int _heartbeatTimeout = 90;
        private Thread _connectionMonitorThread;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockerConnectionMonitor = new object();
        private volatile bool _connectionLost;
        private const int _connectionTimeout = 30000;
        #endregion

        /// <summary>
        /// Creates an instance of a websockets brokerage
        /// </summary>
        /// <param name="wssUrl">Websockets base url</param>
        /// <param name="websocket">Websocket client instance</param>
        /// <param name="restClient">Rest client instance</param>
        /// <param name="apiKey">Brokerage api auth key</param>
        /// <param name="apiSecret">Brokerage api auth secret</param>
        /// <param name="market">Name of market</param>
        /// <param name="name">Name of brokerage</param>
        public BaseWebsocketsBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string market, string name) : base(name)
        {
            WebSocket = websocket;

            WebSocket.Initialize(wssUrl);

            WebSocket.Message += OnMessage;
            WebSocket.Error += OnError;

            RestClient = restClient;
            _market = market;
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
            WebSocket.Connect();
            Wait(_connectionTimeout, () => WebSocket.IsOpen);

            _cancellationTokenSource = new CancellationTokenSource();
            _connectionMonitorThread = new Thread(() =>
            {
                var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
                double nextReconnectionAttemptSeconds = 1;

                lock (_lockerConnectionMonitor)
                {
                    LastHeartbeatUtcTime = DateTime.UtcNow;
                }

                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        if (WebSocket.IsOpen)
                        {
                            LastHeartbeatUtcTime = DateTime.UtcNow;
                        }

                        TimeSpan elapsed;
                        lock (_lockerConnectionMonitor)
                        {
                            elapsed = DateTime.UtcNow - LastHeartbeatUtcTime;
                        }

                        if (!_connectionLost && elapsed > TimeSpan.FromSeconds(_heartbeatTimeout))
                        {

                            if (WebSocket.IsOpen)
                            {
                                // connection is still good
                                LastHeartbeatUtcTime = DateTime.UtcNow;
                            }
                            else
                            {
                                _connectionLost = true;
                                nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                                OnMessage(BrokerageMessageEvent.Disconnected("Connection with server lost. This could be because of internet connectivity issues."));
                            }
                        }
                        else if (_connectionLost)
                        {
                            try
                            {
                                if (elapsed <= TimeSpan.FromSeconds(_heartbeatTimeout))
                                {
                                    _connectionLost = false;
                                    nextReconnectionAttemptSeconds = 1;

                                    OnMessage(BrokerageMessageEvent.Reconnected("Connection with server restored."));
                                }
                                else
                                {
                                    if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                    {
                                        try
                                        {
                                            Reconnect();
                                        }
                                        catch (Exception err)
                                        {
                                            // double the interval between attempts (capped to 1 minute)
                                            nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);
                                            nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);
                                            Log.Error(err);
                                        }
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Log.Error(exception);
                            }
                        }

                        Thread.Sleep(10000);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            }) { IsBackground = true };
            _connectionMonitorThread.Start();
            while (!_connectionMonitorThread.IsAlive)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public override void Disconnect()
        {
            // request and wait for thread to stop
            _cancellationTokenSource?.Cancel();
            _connectionMonitorThread?.Join();
        }

        /// <summary>
        /// Handles websocket errors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnError(object sender, WebSocketError e)
        {
            Log.Error(e.Exception, "WebSocketsBrokerage Web Exception:  ");
        }

        /// <summary>
        /// Handles reconnections in the event of connection loss
        /// </summary>
        protected virtual void Reconnect()
        {
            if (WebSocket.IsOpen)
            {
                // connection is still good
                LastHeartbeatUtcTime = DateTime.UtcNow;
                return;
            }

            Log.Trace($"BaseWebsocketsBrokerage(): Reconnecting... IsConnected: {IsConnected}");
            var subscribed = GetSubscribed();

            WebSocket.Error -= this.OnError;
            try
            {
                //try to clean up state
                if (IsConnected)
                {
                    WebSocket.Close();
                    Wait(_connectionTimeout, () => !WebSocket.IsOpen);
                }
                if (!IsConnected)
                {
                    WebSocket.Connect();
                    Wait(_connectionTimeout, () => WebSocket.IsOpen);
                }
            }
            finally
            {
                WebSocket.Error += this.OnError;
                this.Subscribe(subscribed);
            }
        }

        private void Wait(int timeout, Func<bool> state)
        {
            var StartTime = Environment.TickCount;
            do
            {
                if (Environment.TickCount > StartTime + timeout)
                {
                    throw new Exception("Websockets connection timeout.");
                }
                Thread.Sleep(1);
            }
            while (!state());
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
        protected virtual IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();
            lock (ChannelList)
            {
                foreach (var item in ChannelList)
                {
                    list.Add(Symbol.Create(item.Value.Symbol, SecurityType.Forex, _market));
                }
            }
            return list;
        }

        /// <summary>
        /// Represents a subscription channel
        /// </summary>
        public class Channel
        {
            /// <summary>
            /// The name of the channel
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// The ticker symbol of the channel
            /// </summary>
            public string Symbol { get; set; }
        }

    }

}
