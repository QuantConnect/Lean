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
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool HasSubscribers
        {
            get;
            set;
        }
        public IApi Api { get; private set; }
        public AlgorithmNodePacket Job { get; private set; }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        /// <param name="initializeParameters">The parameters required for initialization</param>
        public void Initialize(MessagingHandlerInitializeParameters initializeParameters)
        {
            //
            Api = initializeParameters.Api;
        }

        /// <summary>
        /// Set the messaging channel
        /// </summary>
        public virtual void SetAuthentication(AlgorithmNodePacket job)
        {
            Job = job;
        }

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
                    var rstack = (!string.IsNullOrEmpty(runtime.StackTrace) ? (Environment.NewLine + " " + runtime.StackTrace) : string.Empty);
                    Log.Error(runtime.Message + rstack);
                    break;

                case PacketType.HandledError:
                    var handled = (HandledErrorPacket)packet;
                    var hstack = (!string.IsNullOrEmpty(handled.StackTrace) ? (Environment.NewLine + " " + handled.StackTrace) : string.Empty);
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

                        var statisticsStr = $"{Environment.NewLine}" +
                            $"{string.Join(Environment.NewLine, result.Results.Statistics.Select(x => $"STATISTICS:: {x.Key} {x.Value}"))}";
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
            var ftp = notification as NotificationFtp;
            if (ftp != null)
            {
                Ftp(ftp);
                return;
            }

            if (!notification.CanSend())
            {
                Log.Error("Messaging.SendNotification(): Send not implemented for notification of type: " + notification.GetType().Name);
                return;
            }
            notification.Send();
        }

        /// <summary>
        /// Send a telegram notification triggered during live trading from a user algorithm.
        /// </summary>
        /// <param name="notification">Notification object class</param>
        private void Ftp(NotificationFtp notification)
        {
            Log.Trace("Messaging.Cloud.Ftp(): Sending Notification to " + notification.Hostname);

            SendNotificationApi(notification);
        }

        private void SendNotificationApi(Notification notification)
        {
            var response = Api.SendNotification(notification, 18373790);
            if (response == null || !response.Success)
            {
                var message = response == null ? "empty API response" : string.Join("-", response.Errors);
                throw new Exception($"Error sending '{notification.GetType().Name}': {message}");
            }
        }

        /// <summary>
        /// Dispose of any resources
        /// </summary>
        public void Dispose()
        {
        }
    }
}
