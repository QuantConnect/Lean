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
using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Wrapper for WebSocket4Net to enhance testability
    /// </summary>
    public class WebSocketWrapper : IWebSocket
    {

        WebSocket wrapped;
        private string _url;

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        public void Initialize(string url)
        {
            _url = url;
            wrapped = new WebSocket(url);
#if DEBUG
            wrapped.AllowUnstrustedCertificate = true;
#endif
            wrapped.EnableAutoSendPing = true;
        }

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        public void Send(string data)
        {
            wrapped.Send(data);
        }

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        public void Connect()
        {
            if (!this.IsOpen)
            {
                wrapped.Open();
            }
        }

        /// <summary>
        /// Wraps Close method
        /// </summary>
        public void Close()
        {
            wrapped.Close();
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsOpen
        {
            get { return wrapped.State == WebSocketState.Open; }
        }

        /// <summary>
        /// Wraps IsAlive
        /// </summary>
        public bool IsConnecting
        {
            get { return wrapped.State == WebSocketState.Connecting; }
        }

        /// <summary>
        /// Returns wrapped instance
        /// </summary>
        public WebSocket Instance { get { return wrapped; } }

        /// <summary>
        /// Wraps Url
        /// </summary>
        public Uri Url
        {
            get { return new Uri(_url); }
        }

        /// <summary>
        /// Wraps message event
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> OnMessage
        {
            add { wrapped.MessageReceived += value; }
            remove { wrapped.MessageReceived -= value; }
        }

        /// <summary>
        /// Wraps error event
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError
        {
            add { wrapped.Error += value; }
            remove { wrapped.Error -= value; }
        }

        /// <summary>
        /// Wraps open method
        /// </summary>
        public event EventHandler OnOpen
        {
            add { wrapped.Opened += value; }
            remove { wrapped.Opened -= value; }
        }

    }
}
