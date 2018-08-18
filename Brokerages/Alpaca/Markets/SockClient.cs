using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Alpaca streaming API.
    /// </summary>
    public sealed partial class SockClient : IDisposable
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
            :this (
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
			if (keyId == null) throw new ArgumentException(nameof(keyId));
			_keyId = keyId;
			if (secretKey == null) throw new ArgumentException(nameof(secretKey));
			_secretKey = secretKey;

            alpacaRestApi = alpacaRestApi ?? new Uri("https://api.alpaca.markets");

            var uriBuilder = new UriBuilder(alpacaRestApi)
            {
                Scheme = alpacaRestApi.Scheme == "http" ? "ws" : "wss"
            };
            uriBuilder.Path += "/stream";

            _webSocket = new WebSocket(uriBuilder.Uri.ToString());

            _webSocket.Opened += handleOpened;
            _webSocket.Closed += handleClosed;

            _webSocket.DataReceived += handleDataReceived;
            _webSocket.Error += (sender, args) => OnError?.Invoke(args.Exception);
        }

        /// <summary>
        /// Occurrs when new account update received from stream.
        /// </summary>
        public event Action<IAccountUpdate> OnAccountUpdate;

        /// <summary>
        /// Occurrs when new trade update received from stream.
        /// </summary>
        public event Action<ITradeUpdate> OnTradeUpdate;

        /// <summary>
        /// Occurrs when stream successfully connected.
        /// </summary>
        public event Action<AuthStatus> Connected;

        /// <summary>
        /// Occurrs when any error happened in stream.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Opens connection to Alpaca streaming API.
        /// </summary>
        /// <returns>Waitable task object for handling action completion in asyncronious mode.</returns>
        public Task ConnectAsync()
        {
            return Task.Run(() => _webSocket.Open());
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
            _webSocket?.Dispose();
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
            DataReceivedEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Data);
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
            JsonTradeUpdate update)
        {
            OnTradeUpdate?.Invoke(update);
        }

        private void handleAccountUpdates(
            JsonAccountUpdate update)
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
