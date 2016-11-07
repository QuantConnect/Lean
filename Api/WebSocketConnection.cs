using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace QuantConnect.Api
{
    public class WebSocketConnection
    {
        private EventHandler _updateSubscriptions;
        private readonly ConcurrentQueue<BaseData> _baseDataFromServer = new ConcurrentQueue<BaseData>();
        private readonly object _lockerSubscriptions = new object();
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();

        private volatile bool _connectionOpen;
        private readonly string _liveDataUrl = Config.Get("live-data-url", "https://www.quantconnect.com/api/v2/live/data");
        private readonly int _liveDataPort = Config.GetInt("live-data-port", 443);
        private const int MaxRetryAttempts = 10;

        private readonly int _userId;
        private readonly string _token;

        /// <summary>
        /// Initialize a new WebSocketConnection instance
        /// </summary>
        /// <param name="userId">QuantConnect user id</param>
        /// <param name="token">QuantConnect Api Token</param>
        public WebSocketConnection(int userId, string token)
        {
            _userId = userId;
            _token = token;
        }

        /// <summary>
        /// Get queued data that's been returned from the live server
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseData> GetLiveData()
        {
            lock (_baseDataFromServer)
            {
                List<BaseData> baseData = new List<BaseData>();
                while (!_baseDataFromServer.IsEmpty)
                {
                    BaseData b;
                    if (_baseDataFromServer.TryDequeue(out b))
                        baseData.Add(b);
                }
                return baseData;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(IEnumerable<Symbol> symbols)
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

                Log.Trace("WebSocketConnection.Subscribe(): Subscribed to: {0}", string.Join(",", symbolsToSubscribe.Select(x => x.Value)));
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
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(IEnumerable<Symbol> symbols)
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

                Log.Trace("WebSocketConnection.Unsubscribe(): Unsubscribed from : {0}", string.Join(",", symbolsToUnsubscribe.Select(x => x.Value)));
            }

            if (!TryOpenSocketConnection())
            {
                // The websocket is already open
                OnUpdate();
            }
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
        /// Attempt to build the websocket connection to the server
        /// </summary>
        public bool TryOpenSocketConnection()
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

                        SHA1 sha = new SHA1CryptoServiceProvider();
                        var timeStamp = DateTime.Now.ToString(DateFormat.EightCharacter);
                        var hash = Convert.ToBase64String(
                                        sha.ComputeHash(
                                                Encoding.UTF8.GetBytes(
                                                    Convert.ToBase64String(
                                                        Encoding.UTF8.GetBytes("password" + ":" + timeStamp)))));

                        ws.SetCookie(new Cookie("timeStamp", timeStamp));
                        ws.SetCookie(new Cookie("token", hash));

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
