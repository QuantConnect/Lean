/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made from original:
 *   - Removed Nullable reference type definitions for compatibility with C# 6
 *   - Moved the `HandleConnected` method from inside `ConnectAndAuthenticateAsync`
 *     to its own method. A new member variable was made in order to be able to
 *     signal that we've authenticated, and be able to unsubscribe the event handler.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for websocket streaming APIs.
    /// </summary>
    public abstract class StreamingClientBase<TConfiguration> : IDisposable
        where TConfiguration : StreamingClientConfiguration
    {
        private readonly SynchronizationQueue _queue = new SynchronizationQueue();

        private readonly IWebSocket _webSocket;
        internal readonly TConfiguration Configuration;

        /// <summary>
        /// Creates new instance of <see cref="StreamingClientBase{TConfiguration}"/> object.
        /// </summary>
        /// <param name="configuration"></param>
        protected StreamingClientBase(
            TConfiguration configuration)
        {
            Configuration = configuration.EnsureNotNull(nameof(configuration));
            Configuration.EnsureIsValid();

            _webSocket = configuration.CreateWebSocket();

            _webSocket.Open += OnOpened;
            _webSocket.Closed += OnClosed;

            _webSocket.Message += OnMessage;

            _webSocket.Error += HandleError;
            _queue.OnError += HandleQueueError;
        }

        /// <summary>
        /// Occured when stream successfully connected.
        /// </summary>
        public event Action<AuthStatus> Connected;

        /// <summary>
        /// Occured when underlying web socket successfully opened.
        /// </summary>
        public event Action SocketOpened;

        /// <summary>
        /// Occured when underlying web socket successfully closed.
        /// </summary>
        public event Action SocketClosed;

        /// <summary>
        /// Occured when any error happened in stream.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Opens connection to a streaming API.
        /// </summary>
        public void Connect() => _webSocket.Connect();

        /// <summary>
        /// Closes connection to a streaming API.
        /// </summary>
        public void Disconnect() => _webSocket.Close();

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handles <see cref="IWebSocket.Open"/> event.
        /// </summary>
        protected virtual void OnOpened(object sender, EventArgs e) => SocketOpened?.Invoke();

        /// <summary>
        /// Handles <see cref="IWebSocket.Closed"/> event.
        /// </summary>
        protected virtual void OnClosed(object sender, WebSocketCloseData e) => SocketClosed?.Invoke();

        /// <summary>
        /// Handles <see cref="IWebSocket.Message"/> event.
        /// </summary>
        protected virtual void OnMessage(object sender, WebSocketMessage message)
        {
        }

        /// <summary>
        /// Implement <see cref="IDisposable"/> pattern for inheritable classes.
        /// </summary>
        /// <param name="disposing">If <c>true</c> - dispose managed objects.</param>
        protected virtual void Dispose(
            Boolean disposing)
        {
            if (!disposing ||
                _webSocket == null)
            {
                return;
            }

            _webSocket.Open -= OnOpened;
            _webSocket.Closed -= OnClosed;

            _webSocket.Message -= OnMessage;

            _webSocket.Error -= HandleError;
            _queue.OnError -= HandleQueueError;

            _queue.Dispose();
        }

        /// <summary>
        /// Handles single incoming message. Select handler from generic handlers map
        /// <paramref name="handlers"/> using <paramref name="messageType"/> parameter
        /// as a key and pass <paramref name="message"/> parameter as value into the
        /// selected handler. All exceptions are caught inside this method and reported
        /// to client via standard <see cref="OnError"/> event.
        /// </summary>
        /// <param name="handlers">Message handlers map.</param>
        /// <param name="messageType">Message type for selecting handler from map.</param>
        /// <param name="message">Message data for processing by selected handler.</param>
        [SuppressMessage(
            "Design", "CA1031:Do not catch general exception types",
            Justification = "Expected behavior - we report exceptions via OnError event.")]
        protected void HandleMessage<TKey>(
            IDictionary<TKey, Action<JToken>> handlers,
            TKey messageType,
            JToken message)
            where TKey : class
        {
            try
            {
                Action<JToken> handler;
                if (handlers != null &&
                    handlers.TryGetValue(messageType, out handler))
                {
                    _queue.Enqueue(() => handler(message));
                }
                else
                {
                    var errorMessage = $"Unexpected message type '{messageType}' received.";
                    HandleError(null, new WebSocketError(errorMessage, new InvalidOperationException(errorMessage)));
                }
            }
            catch (Exception exception)
            {
                HandleError(null, new WebSocketError(exception.Message, exception));
            }
        }

        /// <summary>
        /// Raises <see cref="Connected"/> event with specified <paramref name="authStatus"/> value.
        /// </summary>
        /// <param name="authStatus">Authentication status (protocol level) of client.</param>
        protected void OnConnected(
            AuthStatus authStatus) =>
            Connected?.Invoke(authStatus);

        /// <summary>
        /// Handles <see cref="SynchronizationQueue.OnError"/> event.
        /// </summary>
        protected void HandleError(object sender, WebSocketError error)
        {
            OnError?.Invoke(error.Exception);
        }

        /// <summary>
        /// Handles <see cref="IWebSocket.Error"/> event.
        /// </summary>
        /// <param name="exception">Exception for routing into <see cref="OnError"/> event.</param>
        private void HandleQueueError(Exception exception)
        {
            OnError?.Invoke(exception);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        protected void SendAsJsonString(object value)
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
