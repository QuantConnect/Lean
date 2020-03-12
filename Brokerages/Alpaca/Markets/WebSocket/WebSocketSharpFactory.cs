/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
*/

using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using WebSocketSharp;
using System.Threading;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    [SuppressMessage(
        "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
    internal sealed class WebSocketSharpFactory : IWebSocketFactory
    {
        private sealed class WebSocketWrapper : IWebSocket //-V3073
        {
            private readonly WebSocket _webSocket;

            public WebSocketWrapper(
                Uri url)
            {
                _webSocket = new WebSocket(url.ToString())
                {
                    SslConfiguration = {EnabledSslProtocols = SslProtocols.Tls12}
                };

                _webSocket.OnOpen += handleOpened;
                _webSocket.OnClose += handleClosed;

                _webSocket.OnMessage += handleDataReceived;
                _webSocket.OnMessage += handleMessageReceived;

                _webSocket.OnError += handleError;
            }

            public void Dispose()
            {
                if (_webSocket == null)
                {
                    return;
                }

                _webSocket.OnOpen -= handleOpened;
                _webSocket.OnClose -= handleClosed;

                _webSocket.OnMessage -= handleDataReceived;
                _webSocket.OnMessage -= handleMessageReceived;

                _webSocket.OnError -= handleError;

                var disposable = _webSocket as IDisposable;
                disposable?.Dispose();
            }

            public Task OpenAsync(
                CancellationToken cancellationToken)
                => Task.Run(() => _webSocket.Connect(), cancellationToken);

            public Task CloseAsync(
                CancellationToken cancellationToken)
                => Task.Run(() => _webSocket.Close(), cancellationToken);

            public void Send(
                String message) =>
                _webSocket.Send(message);

            public event Action Opened;

            public event Action Closed;

            public event Action<Byte[]> DataReceived;

            public event Action<String> MessageReceived;

            public event Action<Exception> Error;

            private void handleOpened
                (Object sender,
                EventArgs eventArgs) =>
                Opened?.Invoke();

            private void handleClosed(
                Object sender,
                EventArgs eventArgs) =>
                Closed?.Invoke();

            private void handleDataReceived(
                Object sender,
                MessageEventArgs eventArgs) =>
                DataReceived?.Invoke(eventArgs.RawData);

            private void handleMessageReceived(
                Object sender,
                MessageEventArgs eventArgs) =>
                MessageReceived?.Invoke(eventArgs.Data);

            private void handleError(
                Object sender
                , ErrorEventArgs eventArgs) =>
                Error?.Invoke(eventArgs.Exception);
        }

        public IWebSocket CreateWebSocket(
            Uri url) =>
            new WebSocketWrapper(url);
    }
}
