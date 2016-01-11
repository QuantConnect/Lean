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
 *
*/

using System.ComponentModel.Composition;
using QuantConnect.Notifications;
using QuantConnect.Packets;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Messaging System Plugin Interface. 
    /// Provides a common messaging pattern between desktop and cloud implementations of QuantConnect.
    /// </summary>
    [InheritedExport(typeof(IMessagingHandler))]
    public interface IMessagingHandler
    {
        /// <summary>
        /// Gets or sets whether this messaging handler has any current subscribers.
        /// When set to false, messages won't be sent.
        /// </summary>
        bool HasSubscribers { get; set; }

        /// <summary>
        /// Initialize the Messaging System Plugin. 
        /// </summary>
        void Initialize();

        /// <summary>
        /// Set the user communication channel
        /// </summary>
        /// <param name="channelId">Unique channel id for the communication</param>
        void SetChannel(string channelId);

        /// <summary>
        /// Send any message with a base type of Packet.
        /// </summary>
        /// <param name="packet">Packet of data to send via the messaging system plugin</param>
        void Send(Packet packet);

        /// <summary>
        /// Send a string debug message to the user
        /// </summary>
        /// <param name="line">String message data to send</param>
        /// <param name="projectId">Project id associated with this message</param>
        /// <param name="algorithmId">Algorithm id associated with this message</param>
        /// <param name="compileId">Compile id associated with this message</param>
        void DebugMessage(string line, int projectId, string algorithmId = "", string compileId = "");

        /// <summary>
        /// Send a security types packet: what securities are being used in this algorithm? What markets are we trading?
        /// </summary>
        /// <param name="types">List of security types to be passed to the GUI.</param>
        void SecurityTypes(SecurityTypesPacket types);

        /// <summary>
        /// Send a log message to the final user interface via messaging system plugin.
        /// </summary>
        /// <param name="algorithmId">Algorithm id associated with this log message</param>
        /// <param name="message">String log message to be saved and passed to user interface</param>
        void LogMessage(string algorithmId, string message);

        /// <summary>
        /// Runtime error handler. Triggered when the user algorithm has an unhandled error while the algorithm was running.
        /// </summary>
        /// <param name="algorithmId">Algorithm id associated with this backtest</param>
        /// <param name="error">String error message captured from the unhandled error event</param>
        /// <param name="stacktrace">String stack trace of the runtime error</param>
        void RuntimeError(string algorithmId, string error, string stacktrace = "");

        /// <summary>
        /// Algorithm status change signal from the Lean Engine triggering GUI updates.
        /// </summary>
        /// <param name="algorithmId">Algorithm id associated with this status message</param>
        /// <param name="projectId">The project id associated with this statis message</param>
        /// <param name="status">State(enum) status message</param>
        /// <param name="message">Additional string message information</param>
        void AlgorithmStatus(string algorithmId, int projectId, AlgorithmStatus status, string message = "");

        /// <summary>
        /// Send a backtest result message via the messaging plugin system.
        /// </summary>
        /// <param name="packet">Backtest result packet containing updated chart and progress information</param>
        /// <param name="finalPacket">This is the final packet. Backtests can return before 100% if they have failed or the data does not contain the expected number of days</param>
        void BacktestResult(BacktestResultPacket packet, bool finalPacket = false);

        /// <summary>
        /// Send live trading result packet to the user interface via the messaging plugin system
        /// </summary>
        /// <param name="packet">Live result packet containing live result information to update the GUI</param>
        void LiveTradingResult(LiveResultPacket packet);

        /// <summary>
        /// Send a rate limited email notification triggered during live trading from a user algorithm
        /// </summary>
        /// <param name="notification">Notification object class</param>
        void Email(NotificationEmail notification);

        /// <summary>
        /// Send a rate limited SMS notification triggered duing live trading from a user algorithm.
        /// </summary>
        /// <param name="notification">Notification object class</param>
        void Sms(NotificationSms notification);

        /// <summary>
        /// Send a web REST request notification triggered during live trading from a user algorithm.
        /// </summary>
        /// <param name="notification">Notification object class</param>
        void Web(NotificationWeb notification);
    }
}
