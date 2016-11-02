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
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Packets;
using WebSocketSharp;

namespace QuantConnect.Lean.Engine.DataFeeds.Queues
{
    /// <summary>
    /// Bind to a live data websocket connection
    /// </summary>
    public class ApiDataQueueHandler : IDataQueueHandler
    {
        private EventHandler _updateSubscriptions;
        private volatile bool _connectionOpen;
        private readonly ConcurrentQueue<BaseData> _baseDataFromServer = new ConcurrentQueue<BaseData>();
        private readonly object _lockerSubscriptions = new object();
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();

        private const int MaxRetryAttempts = 10;

        private readonly int _userId = Config.GetInt("job-user-id");
        private readonly string _token = Config.Get("api-access-token");
        private readonly string _liveDataUrl = Config.Get("live-data-url", "https://www.quantconnect.com/api/v2/live/data");
        private readonly int _liveDataPort = Config.GetInt("live-data-port", 443);

        /// <summary>
        /// Get next ticks if they have arrived from the server.
        /// </summary>
        /// <returns>Array of <see cref="BaseData"/></returns>
        public virtual IEnumerable<BaseData> GetNextTicks()
        {
            lock (_baseDataFromServer)
            {
                var copy = _baseDataFromServer.ToArray();
                _baseDataFromServer.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public virtual void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            lock (_lockerSubscriptions)
            {
                var symbolsToSubscribe = (from symbol in symbols
                                          where !_subscribedSymbols.Contains(symbol) && CanSubscribe(symbol)
                                          select symbol).ToList();

                if (symbolsToSubscribe.Count == 0)
                {
                    Log.Trace("ApiDataQueueHandler.Subscribe(): Cannot subscribe to requested symbols. Either symbols are not supported or requested subscriptions already exist.");
                    return;
                }


                foreach (var symbol in symbolsToSubscribe)
                {
                    _subscribedSymbols.Add(symbol);
                }

                Log.Trace("ApiDataQueueHandler.Subscribe(): Subscribed to: {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));
            }

            if (!TryOpenSocketConnection())
            {
                // The websocket is already open
                OnUpdate();
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="job">Job that's being processed processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public virtual void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            lock (_lockerSubscriptions)
            {
                var symbolsToUnsubscribe = (from symbol in symbols
                                            where _subscribedSymbols.Contains(symbol)
                                            select symbol).ToList();

                if (symbolsToUnsubscribe.Count == 0)
                {
                    Log.Trace("ApiDataQueueHandler.Unsubscribe(): Cannot unsubscribe from requested symbols. No existing subscriptions found for: {0}", string.Join(",", symbols.Select(x => x.Value)));
                    return;
                }


                foreach (var symbol in symbolsToUnsubscribe)
                {
                    _subscribedSymbols.Remove(symbol);
                }

                Log.Trace("ApiDataQueueHandler.Unsubscribe(): Unsubscribed from : {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));
            }

            if (!TryOpenSocketConnection())
            {
                // The websocket is already open
                OnUpdate();
            }
        }

        /// <summary>
        /// Attempt to build the websocket connection to the server
        /// </summary>
        private bool TryOpenSocketConnection()
        {
            if (_connectionOpen) return false;

            _connectionOpen = true;

            var cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                var builder = new UriBuilder(new Uri(_liveDataUrl)) { Port = _liveDataPort };
                var webSocketSetupComplete = false;
                using (var ws = new WebSocket(builder.ToString()))
                {
                    var connectionRetryAttempts = 0;

                    while (true)
                    {
                        if (webSocketSetupComplete) continue;

                        // Message received from server
                        ws.OnMessage += (sender, e) =>
                        {
                            lock (_baseDataFromServer)
                            {
                                var baseDatas = DeserializeMessage(e.Data);
                                foreach (var baseData in baseDatas)
                                {
                                    _baseDataFromServer.Enqueue(baseData);
                                }
                            }
                        };

                        // Error has in web socket connection
                        ws.OnError += (sender, e) =>
                        {
                            Log.Error("ApiDataQueueHandler.TryOpenSocketConnection(): Web socket connection error: {0}", e.Message);
                            _connectionOpen = false;
                            if (connectionRetryAttempts < MaxRetryAttempts)
                            {
                                Log.Trace(
                                    "ApiDataQueueHandler.TryOpenSocketConnection(): Attempting to reconnect {0}/{1}",
                                    connectionRetryAttempts, MaxRetryAttempts);

                                connectionRetryAttempts++;
                                ws.Connect();
                            }
                            else
                            {
                                Log.Trace(
                                    "ApiDataQueueHandler.TryOpenSocketConnection(): Could not reconnect to web socket server. " +
                                    "Closing web socket.");

                                _updateSubscriptions = null;
                                cts.Cancel();
                                cts.Dispose();
                            }
                        };

                        // Connection was closed
                        ws.OnClose += (sender, e) =>
                        {
                            Log.Trace(
                                "ApiDataQueueHandler.TryOpenSocketConnection(): Web socket connection closed: {0}, {1}", 
                                e.Code, e.Reason);

                            _connectionOpen = false;

                            if (e.Code == (ushort)CloseStatusCode.Abnormal && connectionRetryAttempts < MaxRetryAttempts)
                            {
                                Log.Trace(
                                    "ApiDataQueueHandler.TryOpenSocketConnection(): Web socket was closed abnormally. " +
                                    "Attempting to reconnect {0}/{1}", 
                                    connectionRetryAttempts, MaxRetryAttempts);

                                connectionRetryAttempts++;
                                ws.Connect();
                            }
                            else
                            {
                                Log.Trace(
                                    "ApiDataQueueHandler.TryOpenSocketConnection(): Could not reconnect to web socket server. " +
                                    "Closing web socket.");

                                _updateSubscriptions = null;
                                cts.Cancel();
                                cts.Dispose();
                            }
                        };

                        // Connection opened
                        ws.OnOpen += (sender, e) =>
                        {
                            lock (_lockerSubscriptions)
                            {
                                ws.Send(JsonConvert.SerializeObject(_subscribedSymbols));
                                Log.Trace(
                                    "ApiDataQueueHandler.TryOpenSocketConnection(): Opened web socket connection to: {0}", 
                                    builder);

                                // reset retry attempts
                                connectionRetryAttempts = 0;
                            }
                        };

                        // subscriptions have been updated
                        _updateSubscriptions += (sender, args) =>
                        {
                            lock (_lockerSubscriptions)
                            {
                                if (ws != null) ws.Send(JsonConvert.SerializeObject(_subscribedSymbols));
                            }
                        };

                        ws.SetCredentials(_userId.ToString(), _token, true);
                        ws.Connect();

                        webSocketSetupComplete = true;
                    }
                }
            }, cts.Token);

            // if we made it this far, we've attempted to open the websocket connection
            return true;
        }

        /// <summary>
        /// Raise event that will change subscription
        /// </summary>
        private void OnUpdate()
        {
            var handler = _updateSubscriptions;
            if (handler != null)
            {
                handler.Invoke(this, null);
            }
        }


        /// <summary>
        /// Returns true if this supports the specified symbol
        /// </summary>
        private static bool CanSubscribe(Symbol symbol)
        {
            // ignore unsupported security types
            if (symbol.ID.SecurityType != SecurityType.Equity &&
                symbol.ID.SecurityType != SecurityType.Cfd &&
                symbol.ID.SecurityType != SecurityType.Forex)
            {
                Log.Trace("ApiDataQueueHandler.CanSubscribe(): Unsupported security type, {0}.", symbol.ID.SecurityType);
                return false;
            }

            // ignore unsupported markets
            if (symbol.ID.Market != Market.Oanda &&
                symbol.ID.Market != Market.FXCM &&
                symbol.ID.Market != Market.USA)
            {
                Log.Trace("ApiDataQueueHandler.CanSubscribe(): Unsupported market, {0}.", symbol.ID.Market);
                return false;
            }

            // ignore universe symbols
            if (symbol.Value.Contains("-UNIVERSE-"))
            {
                Log.Trace("ApiDataQueueHandler.CanSubscribe(): Universe Symbols not supported.");
                return false;
            };

            // If we made it this far, the symbol is supported
            return true;
        }

        /// <summary>
        /// Deserialize the message from the data server
        /// </summary>
        /// <param name="serialized">The data server's message</param>
        /// <returns>An enumerable of base data, if unsuccessful, returns an empty enumerable</returns>
        private static IEnumerable<BaseData> DeserializeMessage(string serialized)
        {
            try
            {
                var deserialized = JsonConvert.DeserializeObject(serialized, JsonSerializerSettings);

                var enumerable = deserialized as IEnumerable<BaseData>;
                if (enumerable != null)
                {
                    return enumerable;
                }

                var data = deserialized as BaseData;
                if (data != null)
                {
                    return new[] { data };
                }
            }
            catch (Exception err)
            {
                Log.Error("ApiDataQueueHandler.DeserializeMessage(): {0}", err);
            }

            return Enumerable.Empty<BaseData>();
        }

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
    }
}
