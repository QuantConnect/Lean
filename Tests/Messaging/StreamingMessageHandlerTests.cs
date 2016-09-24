using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Messaging;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Messaging
{
    [TestFixture, Ignore("This test requires an open TCP to be configured.")]
    public class StreamingMessageHandlerTests
    {
        private readonly string _port = "1234";
        private StreamingMessageHandler _messageHandler;

        [TestFixtureSetUp]
        public void SetUp()
        {
            Config.Set("desktop-http-port", _port);

            _messageHandler = new StreamingMessageHandler();
            _messageHandler.Initialize();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            //
        }

        [Test]
        public void MessageHandler_WillSend_MultipartMessage()
        {
            using (var pullSocket = new PullSocket(">tcp://localhost:" + _port))
            {
                var logPacket = new LogPacket
                {
                    Message = "1"
                };

                var tx = JsonConvert.SerializeObject(logPacket);

                _messageHandler.Transmit(logPacket);

                var message = pullSocket.ReceiveMultipartMessage();

                Assert.IsTrue(message.FrameCount == 1);
                Assert.IsTrue(message[0].ConvertToString() == tx);
            }
        }

        [Test]
        public void MessageHandler_SendsCorrectPackets_ToCorrectRoutes()
        {
            //Allow proper decoding of orders.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };

            // Create list of packets to test
            var debug = new DebugPacket();
            var log = new LogPacket();
            var backtest = new BacktestResultPacket();
            var handled = new HandledErrorPacket();
            var error = new RuntimeErrorPacket();
            var packetList = new List<Packet>
                {
                    log,
                    debug,
                    backtest,
                    handled,
                    error
                };

            using (var pullSocket = new PullSocket(">tcp://localhost:" + _port))
            {
                var count = 0;
                while (count < packetList.Count)
                {
                    _messageHandler.Send(packetList[count]);

                    var message = pullSocket.ReceiveMultipartMessage();

                    var payload = message[0].ConvertToString();
                    var packet = JsonConvert.DeserializeObject<Packet>(payload);

                    Assert.IsTrue(message.FrameCount == 1);

                    if (PacketType.Debug == packet.Type)
                        Assert.IsTrue(payload == JsonConvert.SerializeObject(debug));

                    if (PacketType.HandledError == packet.Type)
                        Assert.IsTrue(payload == JsonConvert.SerializeObject(handled));

                    if (PacketType.BacktestResult == packet.Type)
                        Assert.IsTrue(payload == JsonConvert.SerializeObject(backtest));

                    if (PacketType.RuntimeError == packet.Type)
                        Assert.IsTrue(payload == JsonConvert.SerializeObject(error));

                    if (PacketType.Log == packet.Type)
                        Assert.IsTrue(payload == JsonConvert.SerializeObject(log));

                    count++;
                }
            }
        }

        [Test]
        public void MessageHandler_WillSend_NewBackTestJob_ToCorrectRoute()
        {
            var backtest = new BacktestNodePacket();

            using (var pullSocket = new PullSocket(">tcp://localhost:" + _port))
            {
                _messageHandler.SetAuthentication(backtest);

                var message = pullSocket.ReceiveMultipartMessage();

                var payload = message[0].ConvertToString();
                var packet = JsonConvert.DeserializeObject<Packet>(payload);

                Assert.IsTrue(message.FrameCount == 1);
                Assert.IsTrue(PacketType.BacktestNode == packet.Type);
                Assert.IsTrue(payload == JsonConvert.SerializeObject(backtest));
            }
        }

        [Test]
        public void MessageHandler_WillSend_NewLiveJob_ToCorrectRoute()
        {
            using (var pullSocket = new PullSocket(">tcp://localhost:" + _port))
            {
                _messageHandler.SetAuthentication(new LiveNodePacket());

                var message = pullSocket.ReceiveMultipartMessage();

                var payload = message[0].ConvertToString();
                var packet = JsonConvert.DeserializeObject<Packet>(payload);

                Assert.IsTrue(message.FrameCount == 1);
                Assert.IsTrue(PacketType.LiveNode == packet.Type);
                Assert.IsTrue(payload == JsonConvert.SerializeObject(new LiveNodePacket()));
            }
        }
    }
}