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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Wrapper for System.Net.Websockets.ClientWebSocket to enhance testability
    /// </summary>
    public class WebSocketClientWrapper : IWebSocket
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

                    var count = 0;
                    do
                    {
                        // wait for _client to be not null
                        if (_client != null || _cts.Token.WaitHandle.WaitOne(50))
                        {
                            break;
                        }
                    }
                    while (++count < 100);
                }
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
                    _client?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", _cts.Token).SynchronouslyAwaitTask();

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

            while (!_cts.IsCancellationRequested)
            {
                Log.Trace($"WebSocketClientWrapper.HandleConnection({_url}): Connecting...");

                using (var connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    try
                    {
                        lock(_locker)
                        {
                            _client.DisposeSafely();
                            _client = new ClientWebSocket();
                            _client.ConnectAsync(new Uri(_url), connectionCts.Token).SynchronouslyAwaitTask();
                        }
                        OnOpen();

                        while ((_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseSent) &&
                            !connectionCts.IsCancellationRequested)
                        {
                            var messageData = ReceiveMessage(_client, connectionCts.Token, receiveBuffer);

                            if (messageData.MessageType == WebSocketMessageType.Close)
                            {
                                Log.Trace($"WebSocketClientWrapper.HandleConnection({_url}): WebSocketMessageType.Close - Data: {messageData.Data}");
                                break;
                            }

                            OnMessage(new WebSocketMessage(this, messageData.Data));
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (WebSocketException ex)
                    {
                        OnError(new WebSocketError(ex.Message, ex));
                        connectionCts.Token.WaitHandle.WaitOne(2000);
                    }
                    catch (Exception ex)
                    {
                        OnError(new WebSocketError(ex.Message, ex));
                    }
                    connectionCts.Cancel();
                }
            }
        }

        private MessageData ReceiveMessage(
            WebSocket webSocket,
            CancellationToken ct,
            byte[] receiveBuffer,
            long maxSize = long.MaxValue)
        {
            var buffer = new ArraySegment<byte>(receiveBuffer);

            using (var ms = new MemoryStream())
            {
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

                return new MessageData
                {
                    Data = Encoding.UTF8.GetString(ms.GetBuffer(), 0 , (int)ms.Length),
                    MessageType = result.MessageType
                };
            }
        }

        private class MessageData
        {
            public string Data { get; set; }
            public WebSocketMessageType MessageType { get; set; }
        }
    }
}
