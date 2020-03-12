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
using System.Threading;
using System.Threading.Tasks;
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
        private TaskCompletionSource<AuthStatus> _tcs;
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

            _webSocket.Opened += OnOpened;
            _webSocket.Closed += OnClosed;

            _webSocket.MessageReceived += OnMessageReceived;
            _webSocket.DataReceived += OnDataReceived;

            _webSocket.Error += HandleError;
            _queue.OnError += HandleError;
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
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Awaitable task object for handling action completion in asynchronous mode.</returns>
        public Task ConnectAsync(
            CancellationToken cancellationToken = default(CancellationToken))
            => _webSocket.OpenAsync(cancellationToken);

        /// <summary>
        /// Opens connection to a streaming API and awaits for authentication response.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Awaitable task object for handling client authentication event in asynchronous mode.</returns>
        public async Task<AuthStatus> ConnectAndAuthenticateAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            _tcs = new TaskCompletionSource<AuthStatus>();
            Connected += HandleConnected;

            await ConnectAsync(cancellationToken).ConfigureAwait(false);
            return await _tcs.Task.ConfigureAwait(false);

        }

        /// <summary>
        /// Sets the authentication status once we've connected to the websocket stream
        /// </summary>
        /// <param name="authStatus">Authentication status returned by the server</param>
        private void HandleConnected(AuthStatus authStatus)
        {
            Connected -= HandleConnected;
            _tcs.SetResult(authStatus);
        }

        /// <summary>
        /// Closes connection to a streaming API.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Awaitable task object for handling action completion in asynchronous mode.</returns>
        public Task DisconnectAsync(
            CancellationToken cancellationToken = default(CancellationToken))
            => _webSocket.CloseAsync(cancellationToken);

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Handles <see cref="IWebSocket.Opened"/> event.
        /// </summary>
        protected virtual void OnOpened() => SocketOpened?.Invoke();

        /// <summary>
        /// Handles <see cref="IWebSocket.Closed"/> event.
        /// </summary>
        protected virtual void OnClosed() => SocketClosed?.Invoke();

        /// <summary>
        /// Handles <see cref="IWebSocket.MessageReceived"/> event.
        /// </summary>
        /// <param name="message">Incoming string message for processing.</param>
        protected virtual void OnMessageReceived(
            String message)
        {
        }

        /// <summary>
        /// Handles <see cref="IWebSocket.DataReceived"/> event.
        /// </summary>
        /// <param name="binaryData">Incoming binary data for processing.</param>
        protected virtual void OnDataReceived(
            Byte[] binaryData)
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

            _webSocket.Opened -= OnOpened;
            _webSocket.Closed -= OnClosed;

            _webSocket.MessageReceived -= OnMessageReceived;
            _webSocket.DataReceived -= OnDataReceived;

            _webSocket.Error -= HandleError;
            _queue.OnError -= OnError;

            _webSocket.Dispose();
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
                    HandleError(new InvalidOperationException(
                        $"Unexpected message type '{messageType}' received."));
                }
            }
            catch (Exception exception)
            {
                HandleError(exception);
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
        /// Handles <see cref="IWebSocket.Error"/> event.
        /// </summary>
        /// <param name="exception">Exception for routing into <see cref="OnError"/> event.</param>
        protected void HandleError(
            Exception exception)
        {
            OnError?.Invoke(exception);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        protected void SendAsJsonString(
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
