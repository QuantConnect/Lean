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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Wrapper for WebSocketSharp to enhance testability
    /// </summary>
    public class WebSocketWrapper : IWebSocket
    {

        WebSocket wrapped;

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        public void Initialize(string url)
        {
            wrapped = new WebSocket(url);
            wrapped.EmitOnPing = true;
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
            wrapped.Connect();
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
        public bool IsAlive
        {
            get { return wrapped.IsAlive; }
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
            get { return wrapped.Url; }
        }

        /// <summary>
        /// Wraps read state property
        /// </summary>
        public WebSocketState ReadyState
        {
            get { return wrapped.ReadyState; }
        }

        /// <summary>
        /// Wraps message event
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage
        {
            add { wrapped.OnMessage += value; }
            remove { wrapped.OnMessage -= value; }
        }

        /// <summary>
        /// Wraps error event
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError
        {
            add { wrapped.OnError += value; }
            remove { wrapped.OnError -= value; }
        }

        /// <summary>
        /// Wraps open method
        /// </summary>
        public event EventHandler OnOpen
        {
            add { wrapped.OnOpen += value; }
            remove { wrapped.OnOpen -= value; }
        }

    }
}
