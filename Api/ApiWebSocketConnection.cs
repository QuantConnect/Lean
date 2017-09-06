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
 *
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Util;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace QuantConnect.Api
{
    /// <summary>
    /// Manages the web socket connection for live data
    /// </summary>
    public class ApiWebSocketConnection
    {
        private readonly ConcurrentQueue<BaseData> _baseDataFromServer = new ConcurrentQueue<BaseData>();
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();
        private EventHandler _updateSubscriptions;

        private readonly object _locker = new object();

        private const int MaxRetryAttempts = 10;

        private bool _initialized = false;

        private readonly int _userId;
        private readonly string _token;

        private readonly string _liveDataUrl = Config.Get("live-data-url", "https://www.quantconnect.com/api/v2/live/data");
        private readonly int _liveDataPort = Config.GetInt("live-data-port", 443);
        private WebSocket _ws;
        private UriBuilder _builder;


        /// <summary>
        /// Initialize a new WebSocketConnection instance
        /// </summary>
        /// <param name="userId">QuantConnect user id</param>
        /// <param name="token">QuantConnect Api Token</param>
        public ApiWebSocketConnection(int userId, string token)
        {
            _userId = userId;
            _token = token;
        }

        /// <summary>
        /// Initialize the web socket connection to the live data server
        /// </summary>
        public void Initialize()
        {
            _initialized = true;

            _builder = new UriBuilder(new Uri(_liveDataUrl)) { Port = _liveDataPort };
            _ws = new WebSocket(_builder.ToString());

            var connectionRetryAttempts = 0;

            var timeStamp = (int)Time.TimeStamp();
            var hash = Api.CreateSecureHash(timeStamp, _token);

            _ws.SetCookie(new Cookie("Timestamp", timeStamp.ToString()));
            _ws.SetCookie(new Cookie("hash", hash));
            _ws.SetCookie(new Cookie("uid", _userId.ToString()));

            // Message received from server
            _ws.OnMessage += (sender, e) =>
            {
                lock (_locker)
                {
                    IEnumerable<BaseData> baseDatas = new List<BaseData>();
                    try
                    {
                        baseDatas = BaseData.DeserializeMessage(e.Data);
                    }
                    catch
                    {
                        Log.Error("ApiWebSocketConnection.OnMessage(): An error was received from the server: {0}", e.Data);
                    }
                    
                    foreach (var baseData in baseDatas)
                    {
                        _baseDataFromServer.Enqueue(baseData);
                    }
                }
            };

            // Error has in web socket connection
            _ws.OnError += (sender, e) =>
            {
                Log.Error("WebSocketConnection.Initialize(): Web socket connection error: {0}", e.Message);
                if (!_ws.IsAlive && connectionRetryAttempts < MaxRetryAttempts)
                {
                    Log.Trace(
                        "WebSocketConnection.Initialize(): Attempting to reconnect {0}/{1}",
                        connectionRetryAttempts, MaxRetryAttempts);

                    connectionRetryAttempts++;
                    _ws.Connect();
                }
                else
                {
                    Log.Trace(
                        "WebSocketConnection.Initialize(): Could not reconnect to web socket server. " +
                        "Closing web socket.");

                    if (_updateSubscriptions != null) _updateSubscriptions -= UpdateSubscriptions;
                    _updateSubscriptions = null;
                }
            };

            // Connection was closed
            _ws.OnClose += (sender, e) =>
            {
                Log.Trace(
                    "WebSocketConnection.Initialize(): Web socket connection closed: {0}, {1}",
                    e.Code, e.Reason);

                if (!_ws.IsAlive && connectionRetryAttempts < MaxRetryAttempts && e.Code == (ushort)CloseStatusCode.Abnormal)
                {
                    Log.Error(
                        "WebSocketConnection.Initialize(): Web socket was closed abnormally. " +
                        "Attempting to reconnect {0}/{1}",
                        connectionRetryAttempts, MaxRetryAttempts);

                    connectionRetryAttempts++;
                    _ws.Connect();
                }
                else
                {
                    Log.Trace(
                        "WebSocketConnection.Initialize(): Could not reconnect to web socket server. " +
                        "Closing web socket.");

                    if (_updateSubscriptions != null) _updateSubscriptions -= UpdateSubscriptions;
                    _updateSubscriptions = null;
                }
            };

            // Connection opened
            _ws.OnOpen += (sender, e) =>
            {
                SendSubscription();
                connectionRetryAttempts = 0;
            };

            _updateSubscriptions += UpdateSubscriptions;

            _ws.Connect();
        }

        /// <summary>
        /// Get queued data that's been returned from the live server
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseData> GetLiveData()
        {
            lock (_locker)
            {
                while (!_baseDataFromServer.IsEmpty)
                {
                    BaseData b;
                    if (_baseDataFromServer.TryDequeue(out b))
                        yield return b;
                }
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(IEnumerable<Symbol> symbols)
        {
            lock (_locker)
            {
                var symbolsToSubscribe = (from symbol in symbols
                                          where !_subscribedSymbols.Contains(symbol) && CanSubscribe(symbol)
                                          select symbol).ToList();

                if (symbolsToSubscribe.Count == 0)
                {
                    Log.Trace("WebSocketConnection.Subscribe(): Cannot subscribe to requested symbols. Either symbols are not supported or requested subscriptions already exist.");
                    return;
                }

                foreach (var symbol in symbolsToSubscribe)
                {
                    _subscribedSymbols.Add(symbol);
                }

                Log.Trace("WebSocketConnection.Subscribe(): Subscribed to: {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));
            }

            if (_initialized)
                OnUpdate();
            else 
                Initialize();
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
        {
            lock (_locker)
            {
                var symbolsToUnsubscribe = (from symbol in symbols
                                            where _subscribedSymbols.Contains(symbol)
                                            select symbol).ToList();

                if (symbolsToUnsubscribe.Count == 0)
                {
                    Log.Trace("WebSocketConnection.Unsubscribe(): Cannot unsubscribe from requested symbols. No existing subscriptions found for: {0}", string.Join(",", symbols.Select(x => x.Value)));
                    return;
                }

                foreach (var symbol in symbolsToUnsubscribe)
                {
                    _subscribedSymbols.Remove(symbol);
                }
            }

            if (_initialized)
                OnUpdate();
            else
                Initialize();
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
                symbol.ID.SecurityType != SecurityType.Forex &&
                symbol.ID.SecurityType != SecurityType.Crypto)
            {
                Log.Trace("WebSocketConnection.CanSubscribe(): Unsupported security type, {0}.", symbol.ID.SecurityType);
                return false;
            }

            // ignore unsupported markets
            if (symbol.ID.Market != Market.Oanda &&
                symbol.ID.Market != Market.FXCM &&
                symbol.ID.Market != Market.USA)
            {
                Log.Trace("WebSocketConnection.CanSubscribe(): Unsupported market, {0}.", symbol.ID.Market);
                return false;
            }

            // ignore universe symbols
            if (symbol.Value.Contains("-UNIVERSE-"))
            {
                Log.Trace("WebSocketConnection.CanSubscribe(): Universe Symbols not supported.");
                return false;
            };

            // If we made it this far, the symbol is supported
            return true;
        }

        /// <summary>
        /// Send current list of symbols to libe data server
        /// </summary>
        private void SendSubscription()
        {
            lock (_locker)
            {
                _ws.Send(JsonConvert.SerializeObject(_subscribedSymbols));
                Log.Trace(
                    "WebSocketConnection.SendSubscription(): Sent {0} subscriptions to: {1}",
                    _subscribedSymbols.Count,
                    _builder);
            }
        }

        /// <summary>
        /// Update subscriptions
        /// </summary>
        private void UpdateSubscriptions(object sender, EventArgs eventArgs)
        {
            if (_ws != null) SendSubscription();
        }
    }
}
