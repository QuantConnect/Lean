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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Packets;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Desktop implementation of messaging system for Lean Engine
    /// </summary>
    public class EventMessagingHandler : IMessagingHandler
    {
        private AlgorithmNodePacket _job;

        /// <summary>
        /// Gets or sets whether this messaging handler has any current subscribers.
        /// When set to false, messages won't be sent.
        /// </summary>
        public bool HasSubscribers
        {
            get;
            set;
        }

        /// <summary>
        /// Initialize the Messaging System Plugin. 
        /// </summary>
        public void Initialize()
        {
            //NOP
        }

        /// <summary>
        /// Set the user communication channel
        /// </summary>
        /// <param name="job"></param>
        public void SetAuthentication(AlgorithmNodePacket job)
        {
            _job = job;
        }

        public delegate void DebugEventRaised(DebugPacket packet);
        public event DebugEventRaised DebugEvent;

        public delegate void LogEventRaised(LogPacket packet);
        public event LogEventRaised LogEvent;

        public delegate void RuntimeErrorEventRaised(RuntimeErrorPacket packet);
        public event RuntimeErrorEventRaised RuntimeErrorEvent;

        public delegate void HandledErrorEventRaised(HandledErrorPacket packet);
        public event HandledErrorEventRaised HandledErrorEvent;

        public delegate void BacktestResultEventRaised(BacktestResultPacket packet);
        public event BacktestResultEventRaised BacktestResultEvent;

        /// <summary>
        /// Send any message with a base type of Packet.
        /// </summary>
        /// <param name="packet"></param>
        public void Send(Packet packet)
        {
            //Packets we handled in the UX.
            switch (packet.Type)
            {
                case PacketType.Debug:
                    var debug = (DebugPacket)packet;
                    OnDebugEvent(debug);
                    break;

                case PacketType.Log:
                    var log = (LogPacket)packet;
                    OnLogEvent(log);
                    break;

                case PacketType.RuntimeError:
                    var runtime = (RuntimeErrorPacket)packet;
                    OnRuntimeErrorEvent(runtime);
                    break;

                case PacketType.HandledError:
                    var handled = (HandledErrorPacket)packet;
                    OnHandledErrorEvent(handled);
                    break;

                case PacketType.BacktestResult:
                    var result = (BacktestResultPacket)packet;
                    OnBacktestResultEvent(result);
                    break;
            }

            if (StreamingApi.IsEnabled)
            {
                StreamingApi.Transmit(_job.UserId, _job.Channel, packet);
            }
        }

        /// <summary>
        /// Send any notification with a base type of Notification.
        /// </summary>
        /// <param name="notification">The notification to be sent.</param>
        public void SendNotification(Notification notification)
        {
            var type = notification.GetType();
            if (type == typeof (NotificationEmail) || type == typeof (NotificationWeb) || type == typeof (NotificationSms))
            {
                Log.Error("Messaging.SendNotification(): Send not implemented for notification of type: " + type.Name);
                return;
            }
            notification.Send();
        }

        /// <summary>
        /// Raise a debug event safely
        /// </summary>
        protected virtual void OnDebugEvent(DebugPacket packet)
        {
            if (DebugEvent != null)
            {
                DebugEvent(packet);
            }
        }

        /// <summary>
        /// Raise a log event safely
        /// </summary>
        protected virtual void OnLogEvent(LogPacket packet)
        {
            if (LogEvent != null)
            {
                LogEvent(packet);
            }
        }

        /// <summary>
        /// Raise a handled error event safely
        /// </summary>
        protected virtual void OnHandledErrorEvent(HandledErrorPacket packet)
        {
            if (HandledErrorEvent != null)
            {
                HandledErrorEvent(packet);
            }
        }

        /// <summary>
        /// Raise runtime error safely
        /// </summary>
        protected virtual void OnRuntimeErrorEvent(RuntimeErrorPacket packet)
        {
            if (RuntimeErrorEvent != null)
            {
                RuntimeErrorEvent(packet);
            }
        }

        /// <summary>
        /// Raise a backtest result event safely.
        /// </summary>
        protected virtual void OnBacktestResultEvent(BacktestResultPacket packet)
        {
            if (BacktestResultEvent != null)
            {
                BacktestResultEvent(packet);
            }
        }
    }
}