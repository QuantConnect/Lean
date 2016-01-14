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

using QuantConnect.Interfaces;
using QuantConnect.Notifications;
using QuantConnect.Packets;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class Messaging : IMessagingHandler
    {
        /// <summary>
        /// The default implementation doesn't send messages, so this does nothing.
        /// </summary>
        public bool HasSubscribers
        {
            get; 
            set;
        }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        public void Initialize()
        {
            //
        }

        /// <summary>
        /// Set the messaging channel
        /// </summary>
        public void SetChannel(string channelId)
        {
            //
        }

        /// <summary>
        /// Send a generic base packet without processing
        /// </summary>
        public void Send(Packet packet)
        {
            //
        }

        /// <summary>
        /// Send a debug message packet
        /// </summary>
        public void DebugMessage(string line, int projectId, string algorithmId = "", string compileId = "")
        {
            //
        }

        /// <summary>
        /// Send a security types in algorithm information packet
        /// </summary>
        public void SecurityTypes(SecurityTypesPacket types)
        {
            //
        }

        /// <summary>
        /// Send a log message packet
        /// </summary>
        public void LogMessage(string algorithmId, string message)
        {
            //
        }

        /// <summary>
        /// Send a runtime error packet:
        /// </summary>
        public void RuntimeError(string algorithmId, string error, string stacktrace)
        {
            //
        }

        /// <summary>
        /// Send an algorithm status update
        /// </summary>
        public void AlgorithmStatus(string algorithmId, int projectId, AlgorithmStatus status, string message = "")
        {
            //
        }

        /// <summary>
        /// Send a backtest result packet
        /// </summary>
        public void BacktestResult(BacktestResultPacket packet, bool finalPacket = false)
        {
            //
        }

        /// <summary>
        /// Send a live trading packet result.
        /// </summary>
        public void LiveTradingResult(LiveResultPacket packet)
        {
            //
        }

        /// <summary>
        /// Send a rate limited email notification triggered during live trading from a user algorithm
        /// </summary>
        /// <param name="notification"></param>
        public void Email(NotificationEmail notification)
        {
            //
        }

        /// <summary>
        /// Send a rate limited SMS notification triggered duing live trading from a user algorithm.
        /// </summary>
        /// <param name="notification"></param>
        public void Sms(NotificationSms notification)
        {
            //
        }

        /// <summary>
        /// Send a web REST request notification triggered during live trading from a user algorithm.
        /// </summary>
        /// <param name="notification"></param>
        public void Web(NotificationWeb notification)
        {
            //
        }
    }
}
