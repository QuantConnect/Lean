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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using RestSharp;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexBrokerageWebsocketsTests
    {
        private BitfinexBrokerage _unit;
        private Mock<IRestClient> _rest = new Mock<IRestClient>();
        private readonly Mock<IWebSocket> _wss = new Mock<IWebSocket>();
        private Symbol _symbol;
        private readonly Mock<IAlgorithm> _algo = new Mock<IAlgorithm>();
        private readonly AccountType _accountType = AccountType.Margin;

        [SetUp]
        public void Setup()
        {
            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(_accountType);

            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);
            _rest = new Mock<IRestClient>();
            _symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);

            _wss.Setup(m => m.Connect()).Verifiable();
            _wss.Setup(m => m.Close()).Verifiable();
        }

        [Test]
        public void IsConnectedTest()
        {
            _wss.Setup(w => w.IsOpen).Returns(true);
            Assert.IsTrue(_unit.IsConnected);
            _wss.Setup(w => w.IsOpen).Returns(false);
            Assert.IsFalse(_unit.IsConnected);
        }

        [Test]
        public void DisconnectTest()
        {
            _wss.Setup(w => w.IsOpen).Returns(true);
            _unit.Connect();
            _unit.Disconnect();
            _wss.Verify(w => w.Close(), Times.Once);
        }

        [Test]
        public void DisposeTest()
        {
            _wss.Setup(w => w.IsOpen).Returns(false);
            _wss.Setup(w => w.Connect()).Callback(() => _wss.Setup(w => w.IsOpen).Returns(true));

            _unit.Connect();
            _unit.Dispose();

            _wss.Verify(w => w.Close(), Times.AtLeastOnce);
            _wss.Setup(w => w.IsOpen).Returns(true);
        }

        [Test]
        public void OnMessageTradeTest()
        {
            var brokerId = "2";
            var json = "[0,\"tu\", [\"abc123\",\"1\",\"BTCUSD\",\"1453989092\",\"2\",\"3\",\"4\",\"<ORD_TYPE>\",\"5\",\"6\",\"BTC\"]]";

            BitfinexTestsHelpers.AddOrder(_unit, 1, brokerId, 3);
            var raised = new ManualResetEvent(false);

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(3, e.FillQuantity);
                Assert.AreEqual(4m, e.FillPrice);
                Assert.AreEqual(24m, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Filled, e.Status);
                raised.Set();
            };

            _unit.OnMessage(_unit, new WebSocketMessage(json));
            Assert.IsTrue(raised.WaitOne(100));
        }

        [Test]
        public void OnMessageTradeExponentTest()
        {
            var brokerId = "2256717409";
            var json = "[0,\"tu\", [\"abc123\",\"1\",\"BTCUSD\",\"1453989092 \",\"" + brokerId + "\",\"3\",\"4\",\"<ORD_TYPE>\",\"5\",\"0.000006\",\"USD\"]]";
            BitfinexTestsHelpers.AddOrder(_unit, 1, brokerId, 3);
            var raised = new ManualResetEvent(false);

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(3, e.FillQuantity);
                Assert.AreEqual(4m, e.FillPrice);
                Assert.AreEqual(0.000006m, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Filled, e.Status);
                raised.Set();
            };

            _unit.OnMessage(_unit, new WebSocketMessage(json));
            Assert.IsTrue(raised.WaitOne(100));
        }

        [Test]
        public void OnMessageTradeNegativeTest()
        {
            var brokerId = "2";
            var json = "[0,\"tu\", [\"abc123\",\"1\",\"BTCUSD\",\"1453989092 \",\"" + brokerId + "\",\"3\",\"-0.000004\",\"<ORD_TYPE>\",\"5\",\"6\",\"USD\"]]";
            BitfinexTestsHelpers.AddOrder(_unit, 1, brokerId, 3);

            var raised = new ManualResetEvent(false);

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(3, e.FillQuantity);
                Assert.AreEqual(-0.000004m, e.FillPrice);
                Assert.AreEqual(6m, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Filled, e.Status);
                raised.Set();
            };

            _unit.OnMessage(_unit, new WebSocketMessage(json));
            Assert.IsTrue(raised.WaitOne(100));
        }

        [Test]
        public void OnMessageTickerTest()
        {
            var json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"0\",\"pair\":\"btcusd\"}";

            _unit.OnMessage(_unit, new WebSocketMessage(json));

            json = "[\"0\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";

            _unit.OnMessage(_unit, new WebSocketMessage(json));

            var actual = _unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(0.01m, actual.Price);

            //should not serialize into exponent
            json = "[\"0\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.0000001\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";

            _unit.OnMessage(_unit, new WebSocketMessage(json));

            actual = _unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(0.01m, actual.Price);

            //should not fail due to parse error on superfluous field
            json = "[\"0\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"abc\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";

            _unit.OnMessage(_unit, new WebSocketMessage(json));

            actual = _unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(0.01m, actual.Price);
        }

        [Test]
        public void OnMessageTickerTest2()
        {
            var json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"2\",\"pair\":\"btcusd\"}";

            _unit.OnMessage(_unit, new WebSocketMessage(json));

            json = "[2,432.51,5.79789796,432.74,0.00009992,-6.41,-0.01,432.72,20067.46166511,442.79,427.26]";

            _unit.OnMessage(_unit, new WebSocketMessage(json));

            var actual = _unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(432.625m, actual.Price);
            Assert.AreEqual(5.79789796, ((Tick)actual).BidSize);
            Assert.AreEqual(0.00009992, ((Tick)actual).AskSize);
        }

        [Test]
        public void OnMessageTradeTickerTest()
        {
            var json = "{\"event\":\"subscribed\",\"channel\":\"trades\",\"chanId\":\"5\",\"pair\":\"btcusd\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));

            json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"1\",\"pair\":\"btcusd\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));

            json = "[ 5, 'te', '1234-BTCUSD', 1443659698, 236.42, 0.49064538 ]";
            _unit.OnMessage(_unit, new WebSocketMessage(json));

            json = "[ 5, 'tu', '1234-BTCUSD', 9869875, 1443659698, 987.42, 0.123 ]";
            _unit.OnMessage(_unit, new WebSocketMessage(json));

            var actual = _unit.GetNextTicks().First();
            Assert.AreEqual("BTCUSD", actual.Symbol.Value);
            Assert.AreEqual(236.42m, actual.Price);
            Assert.AreEqual(0.49064538, ((Tick)actual).Quantity);

            //test some channel substitution
            OnMessageTickerTest2();
        }

        [TestCase("20061")]
        [TestCase("20051")]
        public void OnMessageInfoHardResetTest(string code)
        {
            _wss.Setup(w => w.IsOpen).Returns(true);
            _wss.Setup(m => m.Send(It.IsAny<string>())).Verifiable();
            _wss.Setup(m => m.Initialize(It.IsAny<string>())).Verifiable();
            _wss.Setup(m => m.Close()).Callback(() =>
            {
                _wss.Setup(w => w.IsOpen).Returns(false);
                //saves waiting for connection monitor
                _unit.GetType().InvokeMember("Reconnect", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, _unit, null);
            });
            _wss.Setup(m => m.Connect()).Callback(() => _wss.Setup(w => w.IsOpen).Returns(true));

            _unit.Connect();

            //create subs
            var json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"1\",\"pair\":\"btcusd\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));
            json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"2\",\"pair\":\"ethbtc\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));
            json = "{\"event\":\"subscribed\",\"channel\":\"trades\",\"chanId\":\"3\",\"pair\":\"ethusd\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));

            //return ticks for subs.
            json = "[\"1\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";
            _unit.OnMessage(null, new WebSocketMessage(json));
            json = "[\"2\",\"0.02\",\"0.02\",\"0.02\",\"0.02\",\"0.02\",\"0.02\",\"2\",\"0.02\",\"0.02\",\"0.02\"]";
            _unit.OnMessage(null, new WebSocketMessage(json));

            //ensure ticks for subs
            var actual = _unit.GetNextTicks();

            var tick = actual.Where(a => a.Symbol.Value == "ETHBTC").Single();
            Assert.AreEqual(0.02m, tick.Price);

            tick = actual.Where(a => a.Symbol.Value == "BTCUSD").Single();
            Assert.AreEqual(0.01m, tick.Price);


            //trigger reset event
            json = "{\"event\":\"info\",\"code\":" + code + ",\"msg\":\"Resync from the Trading Engine ended\"}";
            _unit.OnMessage(null, new WebSocketMessage(json));

            //return new subs
            json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"2\",\"pair\":\"btcusd\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));
            json = "{\"event\":\"subscribed\",\"channel\":\"ticker\",\"chanId\":\"1\",\"pair\":\"ethbtc\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));
            json = "{\"event\":\"subscribed\",\"channel\":\"trades\",\"chanId\":\"4\",\"pair\":\"ethusd\"}";
            _unit.OnMessage(_unit, new WebSocketMessage(json));

            //return ticks for new subs. The channel is now different
            json = "[\"1\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"0.01\",\"1\",\"0.01\",\"0.01\",\"0.01\"]";
            _unit.OnMessage(null, new WebSocketMessage(json));
            json = "[\"2\",\"0.02\",\"0.02\",\"0.02\",\"0.02\",\"0.02\",\"0.02\",\"2\",\"0.02\",\"0.02\",\"0.02\"]";
            _unit.OnMessage(null, new WebSocketMessage(json));

            //ensure ticks for new subs
            actual = _unit.GetNextTicks();

            tick = actual.Where(a => a.Symbol.Value == "ETHBTC").Single();
            Assert.AreEqual(0.01m, tick.Price);

            tick = actual.Where(a => a.Symbol.Value == "BTCUSD").Single();
            Assert.AreEqual(0.02m, tick.Price);

            _wss.Verify(w => w.Connect(), Times.AtLeastOnce);
            _wss.Verify(w => w.Close(), Times.AtLeastOnce);
        }

        [Test]
        public void OnMessageTradeSplitFillTest()
        {
            var expectedQuantity = 2;
            BitfinexTestsHelpers.AddOrder(_unit, 1, "700658426", expectedQuantity);
            var raised = new ManualResetEvent(false);
            var expectedFee = 1.72366541m;
            decimal actualFee = 0;
            decimal actualQuantity = 0;

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                actualFee += e.OrderFee;
                actualQuantity += e.AbsoluteFillQuantity;

                if (e.Status == Orders.OrderStatus.Filled)
                {
                    Assert.AreEqual(expectedQuantity, actualQuantity);
                    Assert.AreEqual(expectedFee, Math.Round(actualFee, 8));
                    raised.Set();
                }
            };

            foreach (var line in File.ReadLines(Path.Combine("TestData", "bitfinex_fill.json")))
            {
                _unit.OnMessage(_unit, new WebSocketMessage(line));
            }
            Assert.IsTrue(raised.WaitOne(1000));
        }

        [Test]
        public void OnMessageWalletTest()
        {
            var json = "[0,\"ws\", [[\"trading\",\"btc\",\"123.456789\",\"99.99\"], [\"exchange\",\"btc\",\"123.456789\",\"99.99\"]]]";

            var raised = new ManualResetEvent(false);

            _unit.AccountChanged += (s, e) =>
            {
                Assert.AreEqual("BTC", e.CurrencySymbol);
                Assert.AreEqual(123.456789, e.CashBalance);
                raised.Set();
            };

            _unit.OnMessage(_unit, new WebSocketMessage(json));
            Assert.IsTrue(raised.WaitOne(2000));
        }

        [Test]
        public void SubscribeTest()
        {
            _wss.Setup(w => w.IsOpen).Returns(true);
            var actualSymbols = new List<string>();
            var actualChannels = new List<string>();

            _wss.Setup(w => w.Send(It.IsAny<string>())).Callback<string>(c =>
            {
                var actual = JsonConvert.DeserializeObject<dynamic>(c);
                actualSymbols.Add((string)actual.pair);
                actualChannels.Add((string)actual.channel);
            });

            _unit.Subscribe(new[] { Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("UNIVERSE", SecurityType.Crypto, Market.Bitfinex),
                Symbol.Create("ETHBTC", SecurityType.Crypto, Market.Bitfinex)});

            Assert.AreEqual(4, actualSymbols.Count);
            Assert.AreEqual(4, actualChannels.Count);
            CollectionAssert.Contains(actualChannels, "ticker");
            CollectionAssert.Contains(actualChannels, "trades");
            CollectionAssert.Contains(actualSymbols, "BTCUSD");
            CollectionAssert.Contains(actualSymbols, "ETHBTC");
        }

    }
}