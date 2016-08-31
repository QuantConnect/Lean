using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Packets;
using NetMQ;
using NetMQ.Sockets;

namespace QuantConnect.Views
{
    public class DesktopClient
    {
        private bool _stopServer = false;

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

                    // There should only be 2 part messages
                    if (message.FrameCount != 2) continue;

                    var resource = message[0].ConvertToString();
                    var packet = message[1].ConvertToString();


                    if (typeof(LiveNodePacket).Name == resource)
                    {
                        var liveJobModel = JsonConvert.DeserializeObject<LiveNodePacket>(packet);
                        handler.Initialize(liveJobModel);
                        continue;
                    }
                    if (typeof(BacktestNodePacket).Name == resource)
                    {
                        var backtestJobModel = JsonConvert.DeserializeObject<BacktestNodePacket>(packet);
                        handler.Initialize(backtestJobModel);
                        continue;
                    }
                    if (typeof(DebugPacket).Name == resource)
                    {
                        var debugEventModel = JsonConvert.DeserializeObject<DebugPacket>(packet);
                        handler.DisplayDebugPacket(debugEventModel);
                        continue;
                    }

                    if (typeof(HandledErrorPacket).Name == resource)
                    {
                        var handleErrorEventModel = JsonConvert.DeserializeObject<HandledErrorPacket>(packet);
                        handler.DisplayHandledErrorPacket(handleErrorEventModel);
                        continue;
                    }

                    if (typeof(BacktestResultPacket).Name == resource)
                    {
                        var backtestResultEventModel = JsonConvert.DeserializeObject<BacktestResultPacket>(packet);
                        handler.DisplayBacktestResultsPacket(backtestResultEventModel);
                        continue;
                    }


                    if (typeof(RuntimeErrorPacket).Name == resource)
                    {
                        var runtimeErrorEventModel = JsonConvert.DeserializeObject<RuntimeErrorPacket>(packet);
                        handler.DisplayRuntimeErrorPacket(runtimeErrorEventModel);
                        continue;
                    }


                    if (typeof(LogPacket).Name == resource)
                    {
                        var logEventModel = JsonConvert.DeserializeObject<LogPacket>(packet);
                        handler.DisplayLogPacket(logEventModel);
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