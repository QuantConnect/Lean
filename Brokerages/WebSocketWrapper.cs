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
    /// Wrapper for ClientWebSocket to enhance testability
    /// </summary>
    public class WebSocketWrapper : IWebSocket
    {
        private ClientWebSocket _wrapped;
        private string _url;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ArraySegment<byte> _receiveBuffer = new ArraySegment<byte>(new byte[1024 * 8]);

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        public void Initialize(string url)
        {
            if (_wrapped != null)
            {
                throw new InvalidOperationException("WebSocketWrapper has already been initialized for: " + _url);
            }

            _url = url;
            _wrapped = new ClientWebSocket();
        }

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            try
            {
                if (!IsOpen)
                {
                    Log.Trace($"WebSocketWrapper.Send(): Connection not open (IsOpen:{IsOpen}, State:{_wrapped.State}): attempting connection", true);

                    Connect();
                }

                var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));

                _wrapped.SendAsync(sendBuffer, WebSocketMessageType.Text, true, _cancellationTokenSource.Token).SynchronouslyAwaitTask();
            }
            catch (Exception exception)
            {
                OnError(new WebSocketError(exception.Message, exception));
            }
        }

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        public void Connect()
        {
            if (IsOpen)
            {
                return;
            }

            try
            {
                _wrapped.ConnectAsync(new Uri(_url), _cancellationTokenSource.Token).SynchronouslyAwaitTask();

                if (IsOpen)
                {
                    OnOpen();

                    new TaskFactory().StartNew(
                        async () =>
                        {
                            try
                            {
                                await StartReceiving();
                            }
                            catch (Exception exception)
                            {
                                OnError(new WebSocketError(exception.Message, exception));
                            }

                            if (_wrapped.State == WebSocketState.Aborted)
                            {
                                // need to recreate ClientWebSocket instance, cannot be reused

                                Log.Trace($"WebSocketWrapper.Connect(): Connection aborted (IsOpen:{IsOpen}, State:{_wrapped.State}): creating new ClientWebSocket instance", true);

                                await new TaskFactory().StartNew(ResetAndConnect, _cancellationTokenSource.Token);
                            }

                        }, _cancellationTokenSource.Token);
                }
            }
            catch (Exception exception)
            {
                OnError(new WebSocketError(exception.Message, exception));
            }
        }

        private void ResetAndConnect()
        {
            _wrapped = new ClientWebSocket();
            Connect();
        }

        private async Task StartReceiving()
        {
            while (_wrapped.State == WebSocketState.Open)
            {
                using (var stream = new MemoryStream(1024))
                {
                    WebSocketReceiveResult webSocketReceiveResult;
                    do
                    {
                        webSocketReceiveResult = await _wrapped.ReceiveAsync(_receiveBuffer, _cancellationTokenSource.Token);
                        await stream.WriteAsync(_receiveBuffer.Array, _receiveBuffer.Offset, webSocketReceiveResult.Count, _cancellationTokenSource.Token);
                    } while (!webSocketReceiveResult.EndOfMessage);

                    if (webSocketReceiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            OnMessage(new WebSocketMessage(reader.ReadToEnd()));
                        }
                    }
                    else if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        if (webSocketReceiveResult.CloseStatus != null)
                        {
                            OnClose(new WebSocketCloseData(
                                webSocketReceiveResult.CloseStatus.Value,
                                webSocketReceiveResult.CloseStatusDescription));
                        }
                    }
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
                _wrapped.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _cancellationTokenSource.Token);

                OnClose(new WebSocketCloseData(WebSocketCloseStatus.NormalClosure, string.Empty));
            }
            catch (Exception exception)
            {
                OnError(new WebSocketError(exception.Message, exception));
            }
        }

        /// <summary>
        /// Returns true if the WebSocketState is Open
        /// </summary>
        public bool IsOpen => _wrapped.State == WebSocketState.Open;

        /// <summary>
        /// Wraps ReadyState
        /// </summary>
        public WebSocketState ReadyState => _wrapped.State;

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
            //Log.Trace($"WebSocketWrapper.OnMessage(): {e.Message}", true);
            Message?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Error"/> event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnError(WebSocketError e)
        {
            Log.Error(e.Exception, $"WebSocketWrapper.OnError(): (IsOpen:{IsOpen}, State:{_wrapped.State}): {e.Message}", true);
            Error?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Open"/> event
        /// </summary>
        protected virtual void OnOpen()
        {
            Log.Trace($"WebSocketWrapper.OnOpen(): Connection opened (IsOpen:{IsOpen}, State:{_wrapped.State}): {_url}", true);
            Open?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event invocator for the <see cref="Close"/> event
        /// </summary>
        protected virtual void OnClose(WebSocketCloseData e)
        {
            Log.Trace($"WebSocketWrapper.OnClose(): Connection closed (IsOpen:{IsOpen}, State:{_wrapped.State}, Code:{e.Code}, Reason:{e.Reason}): {_url}", true);

            Closed?.Invoke(this, e);

            if (e.Code == WebSocketCloseStatus.EndpointUnavailable)
            {
                new TaskFactory().StartNew(ResetAndConnect, _cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

            _wrapped.DisposeSafely();
        }
    }
}
