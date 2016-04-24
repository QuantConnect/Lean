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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Bitfinex;
using NUnit.Framework;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Reflection;
using Moq;
using QuantConnect.Configuration;
using TradingApi.Bitfinex;
using System.Threading;
using QuantConnect.Securities;
using System.Diagnostics;
using System.IO;
using QuantConnect.Tests.Brokerages.Bitfinex;

namespace QuantConnect.Brokerages.Bitfinex.Tests
{

    [TestFixture()]
    public class BitfinexWebsocketsBrokerageTests
    {

        BitfinexWebsocketsBrokerage unit;
        Mock<IWebSocket> mock = new Mock<IWebSocket>();
        decimal scaleFactor = 100m;

        [SetUp()]
        public void Setup()
        {
            unit = new BitfinexWebsocketsBrokerage("wss://localhost", mock.Object, "abc", "123", "trading",
                new Mock<BitfinexApi>(It.IsAny<string>(), It.IsAny<string>()).Object, scaleFactor, new Mock<ISecurityProvider>().Object);
        }

        [Test()]
        public void IsConnectedTest()
        {
            mock.Setup(w => w.IsAlive).Returns(true);
            Assert.IsTrue(unit.IsConnected);
            mock.Setup(w => w.IsAlive).Returns(false);
            Assert.IsFalse(unit.IsConnected);
        }

        [Test()]
        public void ConnectTest()
        {
            mock.Setup(m => m.Connect()).Verifiable();
            mock.Setup(m => m.OnMessage(It.IsAny<EventHandler<WebSocketSharp.MessageEventArgs>>())).Verifiable();

            unit.Connect();
            mock.Verify();
        }

        [Test()]
        public void DisconnectTest()
        {
            mock.Setup(m => m.Close()).Verifiable();
            unit.Connect();
            unit.Disconnect();
            mock.Verify();
        }

        [Test()]
        public void DisposeTest()
        {
            mock.Setup(m => m.Close()).Verifiable();
            unit.Connect();
            unit.Dispose();
            mock.Verify();
            unit.Disconnect();
        }

        [Test()]
        public void OnMessageTradeTest()
        {
            string brokerId = "2";
            string json = "[0,\"tu\", [\"abc123\",\"1\",\"BTCUSD\",\"1453989092 \",\"" + brokerId + "\",\"3\",\"4\",\"<ORD_TYPE>\",\"5\",\"6\",\"BTC\"]]";

            BitfinexTestsHelpers.AddOrder(unit, 1, brokerId, scaleFactor, 300);
            ManualResetEvent raised = new ManualResetEvent(false);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(300, e.FillQuantity);
                Assert.AreEqual(0.04m, e.FillPrice);
                Assert.AreEqual(24m, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Filled, e.Status);
                raised.Set();
            };

            unit.OnMessage(unit, GetArgs(json));
            Assert.IsTrue(raised.WaitOne(1000));

        }

        [Test()]
        public void OnMessageTradeExponentTest()
        {
            string brokerId = "2";
            string json = "[0,\"tu\", [\"abc123\",\"1\",\"BTCUSD\",\"1453989092 \",\"" + brokerId + "\",\"3\",\"4\",\"<ORD_TYPE>\",\"5\",\"0.000006\",\"USD\"]]";
            BitfinexTestsHelpers.AddOrder(unit, 1, brokerId, scaleFactor, 300);
            ManualResetEvent raised = new ManualResetEvent(false);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(300, e.FillQuantity);
                Assert.AreEqual(0.04m, e.FillPrice);
                Assert.AreEqual(0.000006m, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Filled, e.Status);
                raised.Set();
            };

            unit.OnMessage(unit, GetArgs(json));
            Assert.IsTrue(raised.WaitOne(1000));
        }

        [Test()]
        public void OnMessageTradeNegativeTest()
        {
            string brokerId = "2";
            string json = "[0,\"tu\", [\"abc123\",\"1\",\"BTCUSD\",\"1453989092 \",\"" + brokerId + "\",\"3\",\"-0.000004\",\"<ORD_TYPE>\",\"5\",\"6\",\"USD\"]]";
            BitfinexTestsHelpers.AddOrder(unit, 1, brokerId, scaleFactor, 300);

            ManualResetEvent raised = new ManualResetEvent(false);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(300, e.FillQuantity);
                Assert.AreEqual(-0.00000004m, e.FillPrice);
                Assert.AreEqual(6m, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Filled, e.Status);
                raised.Set();
            };

            unit.OnMessage(unit, GetArgs(json));
            Assert.IsTrue(raised.WaitOne(1000));
        }

        [Test()]
        public void OnMessageTickerTest()
        {

            string json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"0\",\"pair\":\"btcusd\"}";

            unit.OnMessage(unit, GetArgs(json));

            json = "[\"0\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";

            unit.OnMessage(unit, GetArgs(json));

            var actual = unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(0.0001m, actual.Price);

            //should not serialize into exponent
            json = "[\"0\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.0000001\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";

            unit.OnMessage(unit, GetArgs(json));

            actual = unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(0.0001m, actual.Price);

            //should not fail due to parse error on superfluous field
            json = "[\"0\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"abc\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";

            unit.OnMessage(unit, GetArgs(json));

            actual = unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(0.0001m, actual.Price);

        }

        [Test()]
        public void OnMessageTickerTest2()
        {

            string json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"2\",\"pair\":\"btcusd\"}";

            unit.OnMessage(unit, GetArgs(json));

            json = "[2,432.51,5.79789796,432.74,0.00009992,-6.41,-0.01,432.72,20067.46166511,442.79,427.26]";

            unit.OnMessage(unit, GetArgs(json));

            var actual = unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(4.32625m, actual.Price);
        }


        [Test()]
        public void OnMessageInfoRestartTest()
        {
            string json = "{\"event\":\"info\",\"code\":\"20051\"}";

            mock.Setup(m => m.Connect()).Verifiable();

            unit.OnMessage(unit, GetArgs(json));

            mock.Verify();
        }

        [Test()]
        public void OnMessageInfoResubscribeTest()
        {
            string json = "{\"event\":\"info\",\"code\":\"20061\"}";

            mock.Setup(m => m.Connect()).Verifiable();

            var brokerageMock = new Mock<BitfinexWebsocketsBrokerage>("wss://localhost", mock.Object, "abc", "123", "trading", new Mock<BitfinexApi>(It.IsAny<string>(), 
                It.IsAny<string>()).Object, 100m, new Mock<ISecurityProvider>().Object);

            brokerageMock.Setup(m => m.Unsubscribe(null, null)).Verifiable();
            brokerageMock.Setup(m => m.Subscribe(null, null)).Verifiable();
            mock.Setup(m => m.Send(It.IsAny<string>())).Verifiable();

            unit.OnMessage(brokerageMock.Object, GetArgs(json));

            mock.Verify();
        }

        [Test()]
        public void OnMessageTradeSplitFillTest()
        {
            int expectedQuantity = 200;

            BitfinexTestsHelpers.AddOrder(unit, 1, "700658426", scaleFactor, expectedQuantity);

            ManualResetEvent raised = new ManualResetEvent(false);

            decimal expectedFee = 1.72366541m;
            decimal actualFee = 0;
            decimal actualQuantity = 0;

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                actualFee += e.OrderFee;
                actualQuantity += e.AbsoluteFillQuantity;

                if (e.Status == Orders.OrderStatus.Filled)
                {
                    raised.Set();
                    Assert.AreEqual(expectedQuantity, actualQuantity);
                    Assert.AreEqual(expectedFee, Math.Round(actualFee, 8));
                }
            };

            foreach (var line in File.ReadLines(Path.Combine("TestData", "btcusd_fill.txt")))
            {
                unit.OnMessage(unit, GetArgs(line));
            }
            Assert.IsTrue(raised.WaitOne(1000));

        }

        [DebuggerStepThrough]
        private MessageEventArgs GetArgs(string json)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            System.Globalization.CultureInfo culture = null;
            MessageEventArgs args = (MessageEventArgs)Activator.CreateInstance(typeof(MessageEventArgs), flags, null, new object[]
            {
                Opcode.Text, System.Text.Encoding.UTF8.GetBytes(json)
            }, culture);

            return args;
        }

    }
}
