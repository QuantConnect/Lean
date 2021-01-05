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
    /// Defines data returned from a web socket close event
    /// </summary>
    public class WebSocketCloseData
    {
        /// <summary>
        /// Gets the status code for the connection close.
        /// </summary>
        public ushort Code { get; }

        /// <summary>
        /// Gets the reason for the connection close.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Gets a value indicating whether the connection has been closed cleanly.
        /// </summary>
        public bool WasClean { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketCloseData"/> class
        /// </summary>
        /// <param name="code">The status code for the connection close</param>
        /// <param name="reason">The reaspn for the connection close</param>
        /// <param name="wasClean">True if the connection has been closed cleanly, false otherwise</param>
        public WebSocketCloseData(ushort code, string reason, bool wasClean)
        {
            Code = code;
            Reason = reason;
            WasClean = wasClean;
        }
    }
}