using System;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Packets;
using NetMQ;
using NetMQ.Sockets;

namespace QuantConnect.Views
{
    public class DesktopClient
    {
        private volatile bool _stopServer = false;

        /// <summary>
        /// This 0MQ Pull socket accepts certain messages from a 0MQ Push socket
        /// </summary>
        /// <param name="port">The port on which to listen</param>
        /// <param name="handler">The handler which will display the repsonses</param>
        public void Run(string port, IDesktopMessageHandler handler)
        {
            //Allow proper decoding of orders.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };

            using (var pullSocket = new PullSocket(">tcp://localhost:" + port))
            {
                while (!_stopServer)
                {
                    var message = pullSocket.ReceiveMultipartMessage();

                    // There should only be 1 part messages
                    if (message.FrameCount != 1) continue;

                    var payload = message[0].ConvertToString();

                    var packet = JsonConvert.DeserializeObject<Packet>(payload);

                    switch (packet.Type)
                    {
                        case PacketType.BacktestNode:
                            var backtestJobModel = JsonConvert.DeserializeObject<BacktestNodePacket>(payload);
                            handler.Initialize(backtestJobModel);
                            break;
                        case PacketType.LiveNode:
                            var liveJobModel = JsonConvert.DeserializeObject<LiveNodePacket>(payload);
                            handler.Initialize(liveJobModel);
                            break;
                        case PacketType.Debug:
                            var debugEventModel = JsonConvert.DeserializeObject<DebugPacket>(payload);
                            handler.DisplayDebugPacket(debugEventModel);
                            break;
                        case PacketType.HandledError:
                            var handleErrorEventModel = JsonConvert.DeserializeObject<HandledErrorPacket>(payload);
                            handler.DisplayHandledErrorPacket(handleErrorEventModel);
                            break;
                        case PacketType.BacktestResult:
                            var backtestResultEventModel = JsonConvert.DeserializeObject<BacktestResultPacket>(payload);
                            handler.DisplayBacktestResultsPacket(backtestResultEventModel);
                            break;
                        case PacketType.RuntimeError:
                            var runtimeErrorEventModel = JsonConvert.DeserializeObject<RuntimeErrorPacket>(payload);
                            handler.DisplayRuntimeErrorPacket(runtimeErrorEventModel);
                            break;
                        case PacketType.Log:
                            var logEventModel = JsonConvert.DeserializeObject<LogPacket>(payload);
                            handler.DisplayLogPacket(logEventModel);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Stop the running of the loop
        /// </summary>
        public void StopServer()
        {
            _stopServer = true;
        }
    }
}