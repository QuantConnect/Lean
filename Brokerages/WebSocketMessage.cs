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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Defines a message received at a web socket
    /// </summary>
    public class WebSocketMessage
    {
        /// <summary>
        /// Gets the sender websocket instance
        /// </summary>
        public IWebSocket WebSocket { get; }

        /// <summary>
        /// Gets the raw message data as text
        /// </summary>
        public WebSocketClientWrapper.MessageData Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketMessage"/> class
        /// </summary>
        /// <param name="webSocket">The sender websocket instance</param>
        /// <param name="data">The message data</param>
        public WebSocketMessage(IWebSocket webSocket, WebSocketClientWrapper.MessageData data)
        {
            WebSocket = webSocket;
            Data = data;
        }
    }
}
