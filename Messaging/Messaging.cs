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
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public class Messaging : IMessagingHandler
    {
        /// <summary>
        /// This implementation ignores the <seealso cref="HasSubscribers"/> flag and
        /// instead will always write to the log.
        /// </summary>
        public bool HasSubscribers { get; set; }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        /// <param name="initializeParameters">The parameters required for initialization</param>
        public void Initialize(MessagingHandlerInitializeParameters initializeParameters)
        {
            //
        }

        /// <summary>
        /// Set the messaging channel
        /// </summary>
        public virtual void SetAuthentication(AlgorithmNodePacket job) { }

        /// <summary>
        /// Send a generic base packet without processing
        /// </summary>
        public virtual void Send(Packet packet)
        {
            switch (packet.Type)
            {
                case PacketType.Debug:
                    var debug = (DebugPacket)packet;
                    Log.Trace("Debug: " + debug.Message);
                    break;

                case PacketType.SystemDebug:
                    var systemDebug = (SystemDebugPacket)packet;
                    Log.Trace("Debug: " + systemDebug.Message);
                    break;

                case PacketType.Log:
                    var log = (LogPacket)packet;
                    Log.Trace("Log: " + log.Message);
                    break;

                case PacketType.RuntimeError:
                    var runtime = (RuntimeErrorPacket)packet;
                    var rstack = (
                        !string.IsNullOrEmpty(runtime.StackTrace)
                            ? (Environment.NewLine + " " + runtime.StackTrace)
                            : string.Empty
                    );
                    Log.Error(runtime.Message + rstack);
                    break;

                case PacketType.HandledError:
                    var handled = (HandledErrorPacket)packet;
                    var hstack = (
                        !string.IsNullOrEmpty(handled.StackTrace)
                            ? (Environment.NewLine + " " + handled.StackTrace)
                            : string.Empty
                    );
                    Log.Error(handled.Message + hstack);
                    break;

                case PacketType.AlphaResult:
                    break;

                case PacketType.BacktestResult:
                    var result = (BacktestResultPacket)packet;

                    if (result.Progress == 1)
                    {
                        var orderHash = result.Results.Orders.GetHash();
                        result.Results.Statistics.Add("OrderListHash", orderHash);

                        var statisticsStr =
                            $"{Environment.NewLine}"
                            + $"{string.Join(Environment.NewLine, result.Results.Statistics.Select(x => $"STATISTICS:: {x.Key} {x.Value}"))}";
                        Log.Trace(statisticsStr);
                    }
                    break;
            }
        }

        /// <summary>
        /// Send any notification with a base type of Notification.
        /// </summary>
        public void SendNotification(Notification notification)
        {
            if (!notification.CanSend())
            {
                Log.Error(
                    "Messaging.SendNotification(): Send not implemented for notification of type: "
                        + notification.GetType().Name
                );
                return;
            }
            notification.Send();
        }

        /// <summary>
        /// Dispose of any resources
        /// </summary>
        public void Dispose() { }
    }
}
