/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Changes from the above source:
 *     The websocket connection now depends on System.Net.WebSockets, not on WebSocket4Net as in the original source
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Alpaca streaming API.
    /// </summary>
    internal sealed class SockClient : IDisposable
    {
        private const int ConnectionTimeout = 30000;

        private readonly WebSocketClientWrapper _webSocket;

        private readonly string _keyId;

        private readonly string _secretKey;

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public bool IsConnected => _webSocket.IsOpen;

        /// <summary>
        /// Creates new instance of <see cref="SockClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        public SockClient(
            string keyId,
            string secretKey,
            string alpacaRestApi = null)
            : this(
                keyId,
                secretKey,
                new Uri(alpacaRestApi ?? "https://api.alpaca.markets"))
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="SockClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        public SockClient(
            string keyId,
            string secretKey,
            Uri alpacaRestApi)
        {
            if (keyId == null)
            {
                throw new ArgumentException(nameof(keyId));
            }
            if (secretKey == null)
            {
                throw new ArgumentException(nameof(secretKey));
            }

            _keyId = keyId;
            _secretKey = secretKey;

            alpacaRestApi = alpacaRestApi ?? new Uri("https://api.alpaca.markets");

            var uriBuilder = new UriBuilder(alpacaRestApi)
            {
                Scheme = alpacaRestApi.Scheme == "http" ? "ws" : "wss"
            };
            uriBuilder.Path += "stream";

            _webSocket = new WebSocketClientWrapper();
            _webSocket.Initialize(uriBuilder.Uri.ToString());

            _webSocket.Open += HandleOpened;
            _webSocket.Closed += HandleClosed;

            _webSocket.Message += HandleDataReceived;
            _webSocket.Error += (sender, args) =>
            {
                OnError?.Invoke(args.Exception);
            };
        }

        /// <summary>
        /// Occured when new account update received from stream.
        /// </summary>
        public event Action<IAccountUpdate> OnAccountUpdate;

        /// <summary>
        /// Occured when new trade update received from stream.
        /// </summary>
        public event Action<ITradeUpdate> OnTradeUpdate;

        /// <summary>
        /// Occured when stream successfully connected.
        /// </summary>
        public event Action<AuthStatus> Connected;

        /// <summary>
        /// Occured when any error happened in stream.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Opens connection to Alpaca streaming API.
        /// </summary>
        public void Connect()
        {
            var connectedEvent = new ManualResetEvent(false);
            EventHandler onOpenAction = (s, e) =>
            {
                connectedEvent.Set();
            };

            _webSocket.Open += onOpenAction;

            try
            {
                _webSocket.Connect();

                if (!connectedEvent.WaitOne(ConnectionTimeout))
                {
                    throw new Exception("SockClient.Connect(): WebSocket connection timeout.");
                }
            }
            finally
            {
                _webSocket.Open -= onOpenAction;

                connectedEvent.DisposeSafely();
            }
        }

        /// <summary>
        /// Closes connection to Alpaca streaming API.
        /// </summary>
        public void Disconnect()
        {
            _webSocket.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_webSocket != null && _webSocket.IsOpen)
            {
                _webSocket.Close();
            }
        }

        private void HandleOpened(object sender, EventArgs e)
        {
            var authenticateRequest = new JsonAuthRequest
            {
                Action = JsonAction.Authenticate,
                Data = new JsonAuthRequest.JsonData()
                {
                    KeyId = _keyId,
                    SecretKey = _secretKey
                }
            };

            SendAsJsonString(authenticateRequest);
        }

        private void HandleClosed(object sender, WebSocketCloseData e)
        {
        }

        private void HandleDataReceived(object sender, WebSocketMessage e)
        {
            try
            {
                var root = JObject.Parse(e.Message);

                var data = root["data"];
                var stream = root["stream"].ToString();

                switch (stream)
                {
                    case "authorization":
                        HandleAuthorization(
                            data.ToObject<JsonAuthResponse>());
                        break;

                    case "listening":
                        Connected?.Invoke(AuthStatus.Authorized);
                        break;

                    case "trade_updates":
                        HandleTradeUpdates(
                            data.ToObject<JsonTradeUpdate>());
                        break;

                    case "account_updates":
                        HandleAccountUpdates(
                            data.ToObject<JsonAccountUpdate>());
                        break;

                    default:
                        OnError?.Invoke(new InvalidOperationException(
                            $"Unexpected message type '{stream}' received."));
                        break;
                }
            }
            catch (Exception exception)
            {
                OnError?.Invoke(exception);
            }
        }

        private void HandleAuthorization(JsonAuthResponse response)
        {
            if (response.Status == AuthStatus.Authorized)
            {
                var listenRequest = new JsonListenRequest
                {
                    Action = JsonAction.Listen,
                    Data = new JsonListenRequest.JsonData()
                    {
                        Streams = new List<String>
                        {
                            "trade_updates",
                            "account_updates"
                        }
                    }
                };

                SendAsJsonString(listenRequest);
            }
            else
            {
                Connected?.Invoke(response.Status);
            }
        }

        private void HandleTradeUpdates(ITradeUpdate update)
        {
            OnTradeUpdate?.Invoke(update);
        }

        private void HandleAccountUpdates(IAccountUpdate update)
        {
            OnAccountUpdate?.Invoke(update);
        }

        private void SendAsJsonString(object value)
        {
            using (var textWriter = new StringWriter())
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(textWriter, value);
                _webSocket.Send(textWriter.ToString());
            }
        }
    }
}
