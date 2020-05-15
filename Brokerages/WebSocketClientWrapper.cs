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
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Wrapper for System.Net.Websockets.ClientWebSocket to enhance testability
    /// </summary>
    public class WebSocketClientWrapper : IWebSocket
    {
        private const int ReceiveBufferSize = 8192;

        private string _url;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private ClientWebSocket _client;

        /// <summary>
        /// Static constructor for the <see cref="WebSocketClientWrapper"/> class
        /// </summary>
        static WebSocketClientWrapper()
        {
            // NET 4.5.2 and below does not enable these more secure protocols by default, so we add them in here
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        public void Initialize(string url)
        {
            _url = url;
        }

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        public async void Send(string data)
        {
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            await _client.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token);
        }

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        public void Connect()
        {
            if (!IsOpen)
            {
                Task.Factory.StartNew(
                    async () =>
                    {
                        while (!_cts.IsCancellationRequested)
                        {
                            using (var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                            {
                                await HandleConnection(connectionCts);
                                connectionCts.Cancel();
                            }
                        }
                    });
            }
        }

        /// <summary>
        /// Wraps Close method
        /// </summary>
        public async void Close()
        {
            if (_client != null)
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token);
            }

            OnClose(new WebSocketCloseData(0, string.Empty, true));
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsOpen => _client != null && _client.State == WebSocketState.Open;

        /// <summary>
        /// Wraps message event
        /// </summary>
        public event EventHandler<WebSocketMessage> Message;

        /// <summary>
        /// Wraps error event
        /// </summary>
        public event EventHandler<WebSocketError> Error;

        /// <summary>
        /// Wraps open method
        /// </summary>
        public event EventHandler Open;

        /// <summary>
        /// Wraps close method
        /// </summary>
        public event EventHandler<WebSocketCloseData> Closed;

        /// <summary>
        /// Event invocator for the <see cref="Message"/> event
        /// </summary>
        protected virtual void OnMessage(WebSocketMessage e)
        {
            //Logging.Log.Trace("WebSocketWrapper.OnMessage(): " + e.Message);
            Message?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Error"/> event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(WebSocketError e)
        {
            Log.Error(e.Exception, $"WebSocketClientWrapper.OnError(): (IsOpen:{IsOpen}, State:{_client.State}): {e.Message}");
            Error?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Open"/> event
        /// </summary>
        protected virtual void OnOpen()
        {
            Log.Trace($"WebSocketClientWrapper.OnOpen(): Connection opened (IsOpen:{IsOpen}, State:{_client.State}): {_url}");
            Open?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event invocator for the <see cref="Close"/> event
        /// </summary>
        protected virtual void OnClose(WebSocketCloseData e)
        {
            Log.Trace($"WebSocketClientWrapper.OnClose(): Connection closed (IsOpen:{IsOpen}, State:{_client.State}): {_url}");
            Closed?.Invoke(this, e);
        }

        private async Task HandleConnection(CancellationTokenSource connectionCts)
        {
            using (_client = new ClientWebSocket())
            {
                try
                {
                    await _client.ConnectAsync(new Uri(_url), connectionCts.Token);
                    OnOpen();

                    while (_client.State == WebSocketState.Open && !connectionCts.IsCancellationRequested)
                    {
                        var messageData = await ReceiveMessage(_client, connectionCts.Token);

                        if (messageData.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }

                        var message = Encoding.UTF8.GetString(messageData.Data);
                        OnMessage(new WebSocketMessage(message));
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    OnError(new WebSocketError(ex.Message, ex));
                }
            }
        }

        private static async Task<MessageData> ReceiveMessage(
            WebSocket webSocket,
            CancellationToken ct,
            long maxSize = long.MaxValue)
        {
            var buffer = new ArraySegment<byte>(new byte[ReceiveBufferSize]);

            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                    if (ms.Length > maxSize)
                    {
                        throw new InvalidOperationException("Maximum size of the message was exceeded.");
                    }
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                return new MessageData
                {
                    Data = ms.ToArray(),
                    MessageType = result.MessageType
                };
            }
        }

        private class MessageData
        {
            public byte[] Data { get; set; }
            public WebSocketMessageType MessageType { get; set; }
        }
    }
}
