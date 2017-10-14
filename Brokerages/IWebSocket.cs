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
using System.Threading.Tasks;
using WebSocket4Net;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Wrapper for WebSocket4Net to enhance testability
    /// </summary>
    public interface IWebSocket
    {

        /// <summary>
        /// Wraps constructor
        /// </summary>
        /// <param name="url"></param>
        void Initialize(string url);

        /// <summary>
        /// Wraps send method
        /// </summary>
        /// <param name="data"></param>
        void Send(string data);

        /// <summary>
        /// Wraps Connect method
        /// </summary>
        void Connect();

        /// <summary>
        /// Wraps Close method
        /// </summary>
        void Close();

        /// <summary>
        /// Wraps IsOpen
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Wraps connection state
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Returns wrapped instance
        /// </summary>
        WebSocket Instance { get; }

        /// <summary>
        /// Wraps Url
        /// </summary>
        Uri Url { get; }

        /// <summary>
        /// on message event
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> OnMessage;

        /// <summary>
        /// On error event
        /// </summary>
        event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// On Open event
        /// </summary>
        event EventHandler OnOpen;
    }
}
