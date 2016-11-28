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
        private readonly List<BaseData> _baseDataFromServer = new List<BaseData>();
        private readonly object _lockerSubscriptions = new object();
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();

        private readonly int _userId = Config.GetInt("job-user-id");
        private readonly string _token = Config.Get("api-access-token");
        private readonly string _liveDataUrl = Config.Get("live-data-url", "ws://127.0.0.1");
        private readonly int _liveDataPort = Config.GetInt("live-data-port", 8080);

        /// <summary>
        /// Get next ticks if they have arrived from the server
        /// </summary>
        /// <returns>Array of <see cref="Tick"/></returns>
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
                    return;

                foreach (var symbol in symbolsToSubscribe)
                {
                    _subscribedSymbols.Add(symbol);
                }

                Log.Trace("ApiDataQueueHanlder subscribed to: {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));
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
                    return;

                foreach (var symbol in symbolsToUnsubscribe)
                {
                    _subscribedSymbols.Remove(symbol);
                }

                Log.Trace("ApiDataQueueHanlder unsubscribed from : {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));
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
                    while (true)
                    {
                        if (webSocketSetupComplete) continue;

                        ws.OnMessage += (sender, e) =>
                        {
                            var baseDatas = DeserializeMessage(e.Data);
                            lock (_baseDataFromServer)
                            {
                                foreach (var baseData in baseDatas)
                                {
                                    _baseDataFromServer.Add(baseData);
                                }
                            }
                        };

                        ws.OnError += (sender, e) =>
                        {
                            Log.Error(String.Format("Web socket connection error: {0}", e.Message));

                            _connectionOpen = false;
                            _updateSubscriptions = null;
                            cts.Cancel();
                            cts.Dispose();
                        };

                        ws.OnClose += (sender, e) =>
                        {
                            Log.Trace(String.Format("Web socket connection closed: {0}, {1}", e.Code, e.Reason));

                            _connectionOpen = false;
                            _updateSubscriptions = null;
                            cts.Cancel();
                            cts.Dispose();
                        };

                        ws.OnOpen += (sender, e) =>
                        {
                            lock (_lockerSubscriptions)
                            {
                                ws.Send(JsonConvert.SerializeObject(_subscribedSymbols));
                                Log.Trace(String.Format("Opened web socket connection to: {0}", builder));
                            }
                        };

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

            // if we made it this far, the websocket connection has been attempted to be opened
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
                return false;
            }

            // ignore unsupported markets
            if (symbol.ID.Market != Market.Oanda &&
                symbol.ID.Market != Market.FXCM &&
                symbol.ID.Market != Market.USA)
            {
                return false;
            }
            
            // ignore universe symbols
            return !symbol.Value.Contains("-UNIVERSE-");
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
                Log.Error(err);
            }

            return Enumerable.Empty<BaseData>();
        }

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
    }
}
