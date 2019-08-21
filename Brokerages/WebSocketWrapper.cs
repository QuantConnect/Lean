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
using QuantConnect.Configuration;
using QuantConnect.Logging;
using WebSocketSharp;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Wrapper for WebSocketSharp to enhance testability
    /// </summary>
    public class WebSocketWrapper : IWebSocket
    {
        private WebSocket _wrapped;
        private string _url;

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
            _wrapped = new WebSocket(url)
            {
                Log =
                {
                    Level = Config.GetBool("websocket-log-trace") ? LogLevel.Trace : LogLevel.Error,

                    // The stack frame number of 3 was derived from the usage of the Logger class in the WebSocketSharp library
                    Output = (data, file) => { Log.Trace($"{WhoCalledMe.GetMethodName(3)}(): {data.Message}", true); }
                }
            };

            _wrapped.OnOpen += (sender, args) => OnOpen();
            _wrapped.OnMessage += (sender, args) => OnMessage(new WebSocketMessage(args.Data));
            _wrapped.OnError += (sender, args) => OnError(new WebSocketError(args.Message, args.Exception));
            _wrapped.OnClose += (sender, args) => OnClose(new WebSocketCloseData(args.Code, args.Reason, args.WasClean));
        }

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            _wrapped.Send(data);
        }

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        public void Connect()
        {
            if (!IsOpen)
            {
                _wrapped.Connect();
            }
        }

        /// <summary>
        /// Wraps Close method
        /// </summary>
        public void Close()
        {
            _wrapped.Close();
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsOpen => _wrapped.IsAlive;

        /// <summary>
        /// Wraps ReadyState
        /// </summary>
        public WebSocketState ReadyState => _wrapped.ReadyState;

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
            Log.Error(e.Exception, $"WebSocketWrapper.OnError(): (IsOpen:{IsOpen}, ReadyState:{_wrapped.ReadyState}): {e.Message}");
            Error?.Invoke(this, e);
        }

        /// <summary>
        /// Event invocator for the <see cref="Open"/> event
        /// </summary>
        protected virtual void OnOpen()
        {
            Log.Trace($"WebSocketWrapper.OnOpen(): Connection opened (IsOpen:{IsOpen}, ReadyState:{_wrapped.ReadyState}): {_url}");
            Open?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event invocator for the <see cref="Close"/> event
        /// </summary>
        protected virtual void OnClose(WebSocketCloseData e)
        {
            Log.Trace($"WebSocketWrapper.OnClose(): Connection closed (IsOpen:{IsOpen}, ReadyState:{_wrapped.ReadyState}, Code:{e.Code}, Reason:{e.Reason}, WasClean:{e.WasClean}): {_url}");
            Closed?.Invoke(this, e);
        }
    }
}
