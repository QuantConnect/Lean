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

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Wrapper class for a Binance websocket connection
    /// </summary>
    public class BinanceWebSocketWrapper : WebSocketClientWrapper
    {
        /// <summary>
        /// The unique Id for the connection
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// The handler for the connection
        /// </summary>
        public IConnectionHandler ConnectionHandler { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinanceWebSocketWrapper"/> class.
        /// </summary>
        public BinanceWebSocketWrapper(IConnectionHandler connectionHandler)
        {
            ConnectionId = Guid.NewGuid().ToString();
            ConnectionHandler = connectionHandler;
        }
    }
}
