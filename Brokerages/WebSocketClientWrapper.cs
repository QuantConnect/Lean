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

using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Wrapper for System.Net.Websockets.ClientWebSocket to enhance testability
    /// </summary>
    public class WebSocketClientWrapper : IWebSocket
    {
        private const int ReceiveBufferSize = 8192;

        private string _url;
        private string _sessionToken;
        private CancellationTokenSource _cts;
        private ClientWebSocket _client;
        private Task _taskConnect;
        private object _connectLock = new object();
        private readonly object _locker = new object();

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url">The target websocket url</param>
        /// <param name="sessionToken">The websocket session token</param>
        public void Initialize(string url, string sessionToken = null)
        {
            _url = url;
            _sessionToken = sessionToken;
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
            lock (_connectLock)
            {
                lock (_locker)
                {
                    if (_cts == null)
                    {
                        _cts = new CancellationTokenSource();

                        _client = null;

                        _taskConnect = Task.Factory.StartNew(
                            () =>
                            {
                                Log.Trace($"WebSocketClientWrapper connection task started: {_url}");

                                try
                                {
                                    HandleConnection();
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e, $"Error in WebSocketClientWrapper connection task: {_url}: ");
                                }

                                Log.Trace($"WebSocketClientWrapper connection task ended: {_url}");
                            },
                            _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }
                }

                var count = 0;
                do
                {
                    // wait for _client to be not null, we need to release the '_locker' lock used by 'HandleConnection'
                    if (_client != null || _cts.Token.WaitHandle.WaitOne(50))
                    {
                        break;
                    }
                }
                while (++count < 100);
            }
        }

        /// <summary>
        /// Wraps Close method
        /// </summary>
        public void Close()
        {
            lock (_locker)
            {
                try
                {
                    try
                    {
                        _client?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token).SynchronouslyAwaitTask();
                    }
                    catch
                    {
                        // ignored
                    }

                    _cts?.Cancel();

                    _taskConnect?.Wait(TimeSpan.FromSeconds(5));

                    _cts.DisposeSafely();
                }
                catch (Exception e)
                {
                    Log.Error($"WebSocketClientWrapper.Close({_url}): {e}");
                }

                _cts = null;
            }

            if (_client != null)
            {
                OnClose(new WebSocketCloseData(0, string.Empty, true));
            }
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsOpen => _client?.State == WebSocketState.Open;

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
            Message?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Error"/> event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(WebSocketError e)
        {
            Log.Error(e.Exception, $"WebSocketClientWrapper.OnError(): (IsOpen:{IsOpen}, State:{_client.State}): {_url}: {e.Message}");
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

        private void HandleConnection()
        {
            var receiveBuffer = new byte[ReceiveBufferSize];

            while (_cts is { IsCancellationRequested: false })
            {
                Log.Trace($"WebSocketClientWrapper.HandleConnection({_url}): Connecting...");

                const int maximumWaitTimeOnError = 120 * 1000;
                const int minimumWaitTimeOnError = 2 * 1000;
                var waitTimeOnError = minimumWaitTimeOnError;
                using var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                try
                {
                    lock (_locker)
                    {
                        _client.DisposeSafely();
                        _client = new ClientWebSocket();
                        if (_sessionToken != null)
                        {
                            _client.Options.SetRequestHeader("x-session-token", _sessionToken);
                        }
                        _client.ConnectAsync(new Uri(_url), connectionCts.Token).SynchronouslyAwaitTask();
                    }
                    OnOpen();

                    while ((_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseSent) &&
                        !connectionCts.IsCancellationRequested)
                    {
                        var messageData = ReceiveMessage(_client, connectionCts.Token, receiveBuffer);

                        if (messageData == null)
                        {
                            break;
                        }

                        // reset wait time
                        waitTimeOnError = minimumWaitTimeOnError;
                        OnMessage(new WebSocketMessage(this, messageData));
                    }
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException ex)
                {
                    OnError(new WebSocketError(ex.Message, ex));
                    connectionCts.Token.WaitHandle.WaitOne(waitTimeOnError);

                    // increase wait time until a maximum value. This is useful during brokerage down times
                    waitTimeOnError += Math.Min(maximumWaitTimeOnError, waitTimeOnError);
                }
                catch (Exception ex)
                {
                    OnError(new WebSocketError(ex.Message, ex));
                }
                connectionCts.Cancel();
            }
        }

        private MessageData ReceiveMessage(
            WebSocket webSocket,
            CancellationToken ct,
            byte[] receiveBuffer,
            long maxSize = long.MaxValue)
        {
            var buffer = new ArraySegment<byte>(receiveBuffer);

            using var ms = new MemoryStream();

            WebSocketReceiveResult result;
            do
            {
                result = webSocket.ReceiveAsync(buffer, ct).SynchronouslyAwaitTask();
                ms.Write(buffer.Array, buffer.Offset, result.Count);
                if (ms.Length > maxSize)
                {
                    throw new InvalidOperationException($"Maximum size of the message was exceeded: {_url}");
                }
            }
            while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Binary)
            {
                return new BinaryMessage
                {
                    Data = ms.ToArray(),
                    Count = result.Count,
                };
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                return new TextMessage
                {
                    Message = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length),
                };
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Log.Trace($"WebSocketClientWrapper.HandleConnection({_url}): WebSocketMessageType.Close - Data: {Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length)}");
                return null;
            }
            return null;
        }

        /// <summary>
        /// Defines a message of websocket data
        /// </summary>
        public abstract class MessageData
        {
            /// <summary>
            /// Type of message
            /// </summary>
            public WebSocketMessageType MessageType { get; set; }
        }

        /// <summary>
        /// Defines a text-Type message of websocket data
        /// </summary>
        public class TextMessage : MessageData
        {
            /// <summary>
            /// Data contained in message
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Constructs default instance of the TextMessage
            /// </summary>
            public TextMessage()
            {
                MessageType = WebSocketMessageType.Text;
            }
        }

        /// <summary>
        /// Defines a byte-Type message of websocket data
        /// </summary>
        public class BinaryMessage : MessageData
        {
            /// <summary>
            /// Data contained in message
            /// </summary>
            public byte[] Data { get; set; }

            /// <summary>
            /// Count of message
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// Constructs default instance of the BinaryMessage
            /// </summary>
            public BinaryMessage()
            {
                MessageType = WebSocketMessageType.Binary;
            }
        }
    }
}
