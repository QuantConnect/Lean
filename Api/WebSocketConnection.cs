using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.API;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Orders;
using RestSharp;
using RestSharp.Authenticators;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace QuantConnect.Api
{
    /// <summary>
    /// Manages the web socket connection for live data
    /// </summary>
    public class WebSocketConnection
    {
        private readonly ConcurrentQueue<BaseData> _baseDataFromServer = new ConcurrentQueue<BaseData>();
        private readonly HashSet<Symbol> _subscribedSymbols = new HashSet<Symbol>();
        private EventHandler _updateSubscriptions;

        private readonly object _locker = new object();
        private volatile bool _connectionOpen;

        private const int MaxRetryAttempts = 10;

        private readonly int _userId;
        private readonly string _token;

        private readonly string _liveDataUrl = Config.Get("live-data-url", "https://www.quantconnect.com/api/v2/live/data");
        private readonly int _liveDataPort   = Config.GetInt("live-data-port", 443);
        private static string _apiEndpoint = "https://www.quantconnect.com/api/v2/";

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
            lock (_locker)
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

                        var timeStamp = (int)Time.TimeStamp();
                        var hash = CreateSecureHash(timeStamp);

                        ws.SetCookie(new Cookie("TimeStamp", timeStamp.ToString()));
                        ws.SetCookie(new Cookie("token", hash));
                        ws.SetCookie(new Cookie("uid", _userId.ToString()));

                        // Message received from server
                        ws.OnMessage += (sender, e) =>
                        {
                            lock (_locker)
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
                            Log.Error("WebSocketConnection.TryOpenSocketConnection(): Web socket connection error: {0}", e.Message);
                            _connectionOpen = false;
                            if (connectionRetryAttempts < MaxRetryAttempts)
                            {
                                Log.Trace(
                                    "WebSocketConnection.TryOpenSocketConnection(): Attempting to reconnect {0}/{1}",
                                    connectionRetryAttempts, MaxRetryAttempts);

                                connectionRetryAttempts++;
                                ws.Connect();
                            }
                            else
                            {
                                Log.Trace(
                                    "WebSocketConnection.TryOpenSocketConnection(): Could not reconnect to web socket server. " +
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
                                "WebSocketConnection.TryOpenSocketConnection(): Web socket connection closed: {0}, {1}",
                                e.Code, e.Reason);

                            _connectionOpen = false;

                            if (e.Code == (ushort)CloseStatusCode.Abnormal && connectionRetryAttempts < MaxRetryAttempts)
                            {
                                Log.Trace(
                                    "WebSocketConnection.TryOpenSocketConnection(): Web socket was closed abnormally. " +
                                    "Attempting to reconnect {0}/{1}",
                                    connectionRetryAttempts, MaxRetryAttempts);

                                connectionRetryAttempts++;
                                ws.Connect();
                            }
                            else
                            {
                                Log.Trace(
                                    "WebSocketConnection.TryOpenSocketConnection(): Could not reconnect to web socket server. " +
                                    "Closing web socket.");

                                _updateSubscriptions = null;
                                cts.Cancel();
                                cts.Dispose();
                            }
                        };

                        // Connection opened
                        ws.OnOpen += (sender, e) =>
                        {
                            lock (_locker)
                            {
                                ws.Send(JsonConvert.SerializeObject(_subscribedSymbols));
                                Log.Trace(
                                    "WebSocketConnection.TryOpenSocketConnection(): Opened web socket connection to: {0}",
                                    builder);

                                // reset retry attempts
                                connectionRetryAttempts = 0;
                            }
                        };

                        // subscriptions have been updated
                        _updateSubscriptions += (sender, args) =>
                        {
                            lock (_locker)
                            {
                                if (ws != null) ws.Send(JsonConvert.SerializeObject(_subscribedSymbols));
                            }
                        };

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
                Log.Error("WebSocketConnection.DeserializeMessage(): {0}", err);
            }

            return Enumerable.Empty<BaseData>();
        }

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        /// <summary>
        /// Generate a secure hash for the authorization headers.
        /// </summary>
        /// <returns>Time based hash of user token and timestamp.</returns>
        private string CreateSecureHash(int timestamp)
        {
            // Create a new hash using current UTC timestamp.
            // Hash must be generated fresh each time.
            var data = string.Format("{0}:{1}", _token, timestamp);
            return SHA256(data);
        }

        /// <summary>
        /// Encrypt the token:time data to make our API hash.
        /// </summary>
        /// <param name="data">Data to be hashed by SHA256</param>
        /// <returns>Hashed string.</returns>
        private string SHA256(string data)
        {
            var crypt = new SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetByteCount(data));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }


        /// <summary>
        /// Check a hash for a specified user and timeStamp
        /// </summary>
        /// <param name="tStamp">TimeStamp as int</param>
        /// <param name="hash">Precomputed hash</param>
        /// <param name="uid">User Id</param>
        /// <returns></returns>
        public static bool CheckCredentials(string uid, int tStamp, string hash)
        {
            var request = new RestRequest("authenticate", Method.GET);
            var client = new RestClient(_apiEndpoint);
            try
            {
                request.AddHeader("Timestamp", tStamp.ToString());
                client.Authenticator = new HttpBasicAuthenticator(uid, hash);

                // Execute the authenticated REST API Call
                var restsharpResponse = client.Execute(request);

                // Use custom converter for deserializing live results data
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Converters = { new LiveAlgorithmResultsJsonConverter(), new OrderJsonConverter() }
                };

                //Verify success
                var result = JsonConvert.DeserializeObject<RestResponse>(restsharpResponse.Content);
                if (!result.Success)
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                Log.Error(err, "Api.CheckCredentials(): Failed to make REST request.");
                return false;
            }
            return true;
        }
    }
}
