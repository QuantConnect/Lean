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
    /// Specifies the type of message received from an IBrokerage implementation
    /// </summary>
    public enum BrokerageMessageType
    {
        /// <summary>
        /// Informational message (0)
        /// </summary>
        Information,

        /// <summary>
        /// Warning message (1)
        /// </summary>
        Warning,

        /// <summary>
        /// Fatal error message, the algo will be stopped (2)
        /// </summary>
        Error,

        /// <summary>
        /// Brokerage reconnected with remote server (3)
        /// </summary>
        Reconnect,

        /// <summary>
        /// Brokerage disconnected from remote server (4)
        /// </summary>
        Disconnect,

        /// <summary>
        /// Action required by the user (5)
        /// </summary>
        ActionRequired,
    }
}
