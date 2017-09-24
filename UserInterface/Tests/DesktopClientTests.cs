using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Moq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Packets;

namespace QuantConnect.Views.Tests
{
    [TestFixture, Ignore("This test requires an open TCP to be configured.")]
    class DesktopClientTests
    {
        private string _port = "1235";
        private Thread _thread;
        private DesktopClient _desktopMessageHandler;

        [TestFixtureSetUp]
        public void Setup()
        {
            _desktopMessageHandler = new DesktopClient();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _desktopMessageHandler.StopServer();
            _thread.Join();
        }

        private void StartClientThread(IDesktopMessageHandler messageHandler)
        {
            _thread = new Thread(() => _desktopMessageHandler.Run(_port, messageHandler));
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        [Test]
        public void DesktopClient_WillAccept_SinglePartMessages()
        {
            Queue<Packet> packets = new Queue<Packet>();
            // Mock the the message handler processor
            var messageHandler = new Mock<IDesktopMessageHandler>();
            messageHandler.Setup(mh => mh.DisplayLogPacket(It.IsAny<LogPacket>())).Callback(
                (LogPacket packet) =>
                {
                    packets.Enqueue(packet);
                }).Verifiable();

            // Setup Client
            StartClientThread(messageHandler.Object);

            using (PushSocket server = new PushSocket("@tcp://*:" + _port))
            {
                var message = new NetMQMessage();

                message.Append(JsonConvert.SerializeObject(new LogPacket()));

                server.SendMultipartMessage(message);
            }

            // Give NetMQ time to send the message
            Thread.Sleep(500);

            Assert.IsTrue(packets.Count == 1);
        }

        [Test]
        public void DesktopClient_WillNotAccept_MoreThanOnePartMessages()
        {
            Queue<Packet> packets = new Queue<Packet>();
            // Mock the the message handler processor
            var messageHandler = new Mock<IDesktopMessageHandler>();
            messageHandler.Setup(mh => mh.DisplayLogPacket(It.IsAny<LogPacket>()))
                .Callback((LogPacket packet) =>
                {
                    packets.Enqueue(packet);
                })
                .Verifiable();

            // Setup Client
            StartClientThread(messageHandler.Object);

            using (PushSocket server = new PushSocket("@tcp://*:" + _port))
            {
                var message = new NetMQMessage();

                message.Append(typeof(LogPacket).Name);
                message.Append(JsonConvert.SerializeObject(new LogPacket()));
                message.Append("hello!");

                server.SendMultipartMessage(message);
            }

            // Give NetMQ time to send the message
            Thread.Sleep(500);

            Assert.IsTrue(packets.Count == 0);
        }

        [Test]
        public void DesktopClient_WillShutDown_WhenStopServerIsCalled_FromAnotherThread()
        {
            Queue<Packet> packets = new Queue<Packet>();
            var messageHandler = new Mock<IDesktopMessageHandler>();
            messageHandler.Setup(mh => mh.DisplayLogPacket(It.IsAny<LogPacket>()))
            .Callback((LogPacket packet) =>
            {
                packets.Enqueue(packet);
            })
            .Verifiable();

            StartClientThread(messageHandler.Object);

            // Try to send a message when the DesktopClient is listening
            using (PushSocket server = new PushSocket("@tcp://*:" + _port))
            {
                var message = new NetMQMessage();

                message.Append(JsonConvert.SerializeObject(new LogPacket()));

                server.SendMultipartMessage(message);
            }

            // Give NetMQ time to send the message
            Thread.Sleep(2000);
            Assert.IsTrue(packets.Count == 1);

            // Shut down the server
            _desktopMessageHandler.StopServer();
            Thread.Sleep(2000);

            // Try to send another message when the DesktopClient is not listening
            using (PushSocket server = new PushSocket("@tcp://*:" + _port))
            {
                var message = new NetMQMessage();

                message.Append(JsonConvert.SerializeObject(new LogPacket()));

                server.SendMultipartMessage(message);
            }

            // Give NetMQ time to send the message
            Thread.Sleep(2000);
            // Nothing should have made it so the count should be the same
            Assert.IsTrue(packets.Count == 1);
        }
    }
}