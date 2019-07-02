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

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides handling of a brokerage or data feed connection
    /// </summary>
    public interface IConnectionHandler : IDisposable
    {
        /// <summary>
        /// Event that fires when a connection loss is detected
        /// </summary>
        event EventHandler ConnectionLost;

        /// <summary>
        /// Event that fires when a lost connection is restored
        /// </summary>
        event EventHandler ConnectionRestored;

        /// <summary>
        /// Event that fires when a reconnection attempt is required
        /// </summary>
        event EventHandler ReconnectRequested;

        /// <summary>
        /// Initializes the connection handler
        /// </summary>
        /// <param name="connectionId">The connection id</param>
        void Initialize(string connectionId);

        /// <summary>
        /// Enables/disables monitoring of the connection
        /// </summary>
        /// <param name="isEnabled">True to enable monitoring, false otherwise</param>
        void EnableMonitoring(bool isEnabled);

        /// <summary>
        /// Notifies the connection handler that new data was received
        /// </summary>
        /// <param name="lastDataReceivedTime">The UTC timestamp of the last data point received</param>
        void KeepAlive(DateTime lastDataReceivedTime);
    }
}
