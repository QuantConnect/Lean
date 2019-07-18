/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
 * Changes from the above source:
 *     The websocket connection now depends on WebSocketSharp, not on WebSocket4Net as in the original source
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Security.Authentication;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Alpaca streaming API.
    /// </summary>
    internal sealed class SockClient : IDisposable
    {
        private readonly WebSocket _webSocket;

        private readonly String _keyId;

        private readonly String _secretKey;

        /// <summary>
        /// Creates new instance of <see cref="SockClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="secretKey">Application secret key.</param>
        /// <param name="alpacaRestApi">Alpaca REST API endpoint URL.</param>
        public SockClient(
            String keyId,
            String secretKey,
            String alpacaRestApi = null)
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
            String keyId,
            String secretKey,
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
            uriBuilder.Path += "/stream";

            _webSocket = new WebSocket(uriBuilder.Uri.ToString());

            _webSocket.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;

            _webSocket.OnOpen += handleOpened;
            _webSocket.OnClose += handleClosed;

            _webSocket.OnMessage += handleDataReceived;
            _webSocket.OnError += (sender, args) =>
            {
                Console.WriteLine(args.Exception);
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
        /// Returns true if the websocket ping-pong handshake was successful
        /// </summary>
        public bool IsAlive => _webSocket.IsAlive;

        /// <summary>
        /// Opens connection to Alpaca streaming API.
        /// </summary>
        /// <returns>Waitable task object for handling action completion in asyncronious mode.</returns>
        public Task ConnectAsync()
        {
            return Task.Run(() => _webSocket.Connect());
        }

        /// <summary>
        /// Closes connection to Alpaca streaming API.
        /// </summary>
        /// <returns>Waitable task object for handling action completion in asyncronious mode.</returns>
        public Task DisconnectAsync()
        {
            return Task.Run(() => _webSocket.Close());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_webSocket != null && _webSocket.ReadyState != WebSocketState.Closing && _webSocket.ReadyState != WebSocketState.Closed)
            {
                _webSocket.Close();
            }
        }

        private void handleOpened(
            Object sender,
            EventArgs e)
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

            sendAsJsonString(authenticateRequest);
        }

        private void handleClosed(
            Object sender,
            EventArgs e)
        {
        }

        private void handleDataReceived(
            Object sender,
            MessageEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.RawData);
            try
            {
                var root = JObject.Parse(message);

                var data = root["data"];
                var stream = root["stream"].ToString();

                switch (stream)
                {
                    case "authorization":
                        handleAuthorization(
                            data.ToObject<JsonAuthResponse>());
                        break;

                    case "listening":
                        Connected?.Invoke(AuthStatus.Authorized);
                        break;

                    case "trade_updates":
                        handleTradeUpdates(
                            data.ToObject<JsonTradeUpdate>());
                        break;

                    case "account_updates":
                        handleAccountUpdates(
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

        private void handleAuthorization(
            JsonAuthResponse response)
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

                sendAsJsonString(listenRequest);
            }
            else
            {
                Connected?.Invoke(response.Status);
            }
        }

        private void handleTradeUpdates(
            ITradeUpdate update)
        {
            OnTradeUpdate?.Invoke(update);
        }

        private void handleAccountUpdates(
            IAccountUpdate update)
        {
            OnAccountUpdate?.Invoke(update);
        }

        private void sendAsJsonString(
            Object value)
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
