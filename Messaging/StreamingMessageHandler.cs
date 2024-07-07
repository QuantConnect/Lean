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
using System.Net;
using System.Net.Sockets;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Notifications;
using QuantConnect.Orders.Serialization;
using QuantConnect.Packets;

namespace QuantConnect.Messaging
{
    /// <summary>
    /// Message handler that sends messages over tcp using NetMQ.
    /// </summary>
    public class StreamingMessageHandler : IMessagingHandler
    {
        private string _port;
        private PushSocket _server;
        private AlgorithmNodePacket _job;
        private OrderEventJsonConverter _orderEventJsonConverter;

        /// <summary>
        /// Gets or sets whether this messaging handler has any current subscribers.
        /// This is not used in this message handler.  Messages are sent via tcp as they arrive
        /// </summary>
        public bool HasSubscribers { get; set; }

        /// <summary>
        /// Initialize the messaging system
        /// </summary>
        /// <param name="initializeParameters">The parameters required for initialization</param>
        public void Initialize(MessagingHandlerInitializeParameters initializeParameters)
        {
            _port = Config.Get("desktop-http-port");
            CheckPort();
            _server = new PushSocket("@tcp://*:" + _port);
        }

        /// <summary>
        /// Set the user communication channel
        /// </summary>
        /// <param name="job"></param>
        public void SetAuthentication(AlgorithmNodePacket job)
        {
            _job = job;
            _orderEventJsonConverter = new OrderEventJsonConverter(job.AlgorithmId);
            Transmit(_job);
        }

        /// <summary>
        /// Send any notification with a base type of Notification.
        /// </summary>
        /// <param name="notification">The notification to be sent.</param>
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
        /// Send all types of packets
        /// </summary>
        public void Send(Packet packet)
        {
            Transmit(packet);
        }

        /// <summary>
        /// Send a message to the _server using ZeroMQ
        /// </summary>
        /// <param name="packet">Packet to transmit</param>
        public void Transmit(Packet packet)
        {
            var payload = JsonConvert.SerializeObject(packet, _orderEventJsonConverter);

            var message = new NetMQMessage();

            message.Append(payload);

            _server.SendMultipartMessage(message);
        }

        /// <summary>
        /// Check if port to be used by the desktop application is available.
        /// </summary>
        private void CheckPort()
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, _port.ToInt32());
                tcpListener.Start();
                tcpListener.Stop();
            }
            catch
            {
                throw new Exception(
                    "The port configured in config.json is either being used or blocked by a firewall."
                        + "Please choose a new port or open the port in the firewall."
                );
            }
        }

        /// <summary>
        /// Dispose any resources used before destruction
        /// </summary>
        public void Dispose() { }
    }
}
