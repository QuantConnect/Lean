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
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Brokerages.Zerodha
{
    /// <summary>
    /// Zerodha Web Socket Client Wrapper
    /// </summary>
    public class ZerodhaWebSocketClientWrapper
    {
        private const int ReceiveBufferSize = 8192;

        private string _url;
        private CancellationTokenSource _cts;
        private ClientWebSocket _client;
        private Task _taskConnect;
        private readonly object _locker = new object();

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
        public void Send(string data)
        {
            lock (_locker)
            {
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
                _client.SendAsync(buffer, WebSocketMessageType.Text, true, _cts.Token).SynchronouslyAwaitTask();
            }
        }

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        public void Connect()
        {
            lock (_locker)
            {
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();

                    _taskConnect = Task.Factory.StartNew(
                        () =>
                        {
                            Log.Trace("ZerodhaWebSocketClientWrapper connection task started.");

                            try
                            {
                                while (!_cts.IsCancellationRequested)
                                {
                                    using (var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                                    {
                                        HandleConnection(connectionCts).SynchronouslyAwaitTask();
                                        connectionCts.Cancel();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "Error in ZerodhaWebSocketClientWrapper connection task");
                            }

                            Log.Trace("ZerodhaWebSocketClientWrapper connection task ended.");
                        },
                        _cts.Token);
                }
            }
        }

        /// <summary>
        /// Wraps Close method
        /// </summary>
        public void Close()
        {
            try
            {
                _client?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token).SynchronouslyAwaitTask();

                _cts?.Cancel();

                _taskConnect?.Wait(TimeSpan.FromSeconds(5));

                _cts.DisposeSafely();
            }
            catch (Exception e)
            {
                Log.Error($"ZerodhaWebSocketClientWrapper.Close(): {e}");
            }

            _cts = null;

            OnClose(new WebSocketCloseData(0, string.Empty, true));
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsOpen => _client != null && _client.State == WebSocketState.Open;

        /// <summary>
        /// Wraps message event
        /// </summary>
        public event EventHandler<MessageData> Message;

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
        /// Wraps ReadyState
        /// </summary>
        public WebSocketState ReadyState => _client.State;

        /// <summary>
        /// Event invocator for the <see cref="Message"/> event
        /// </summary>
        protected virtual void OnMessage(MessageData e)
        {
            Message?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Error"/> event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(WebSocketError e)
        {
            Log.Error(e.Exception, $"ZerodhaWebSocketClientWrapper.OnError(): (IsOpen:{IsOpen}, State:{_client.State}): {e.Message}");
            Error?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Open"/> event
        /// </summary>
        protected virtual void OnOpen()
        {
            Log.Trace($"ZerodhaWebSocketClientWrapper.OnOpen(): Connection opened (IsOpen:{IsOpen}, State:{_client.State}): wss://ws.kite.trade ...");
            Open?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event invocator for the <see cref="Close"/> event
        /// </summary>
        protected virtual void OnClose(WebSocketCloseData e)
        {
            Log.Trace($"ZerodhaWebSocketClientWrapper.OnClose(): Connection closed (IsOpen:{IsOpen}, State:{_client.State}):  wss://ws.kite.trade ...");
            Closed?.Invoke(this, e);
        }

        /// <summary>
        /// Connection Handler 
        /// </summary>
        private async Task HandleConnection(CancellationTokenSource connectionCts)
        {
            using (_client = new ClientWebSocket())
            {
                Log.Trace("ZerodhaWebSocketClientWrapper.HandleConnection(): Connecting to  wss://ws.kite.trade ....");

                try
                {                    
                    _client.Options.SetRequestHeader("X-Kite-Version", "3");

                    await _client.ConnectAsync(new Uri(_url), connectionCts.Token);
                    OnOpen();

                    while ((_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseSent) &&
                        !connectionCts.IsCancellationRequested)
                    {
                        var messageData = await ReceiveMessage(_client, connectionCts.Token);

                        if (messageData.MessageType == WebSocketMessageType.Close)
                        {
                            Log.Trace("ZerodhaWebSocketClientWrapper.HandleConnection(): WebSocketMessageType.Close");
                            return;
                        }

                        //var message = Encoding.UTF8.GetString(messageData.Data);
                        OnMessage(messageData);
                    }
                }
                catch (OperationCanceledException) { }
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
                    MessageType = result.MessageType,
                    Count = result.Count
                };
            }
        }

        
    }

    /// <summary>
    /// Message Data 
    /// </summary>
    public class MessageData
    {
        /// <summary>
        /// Data contained in message
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Type of message
        /// </summary>
        public WebSocketMessageType MessageType { get; set; }

        /// <summary>
        /// Count of message
        /// </summary>
        public int Count { get; internal set; }
    }
}