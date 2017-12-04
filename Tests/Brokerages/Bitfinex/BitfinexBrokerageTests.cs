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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Brokerages.Bitfinex.Rest;
using QuantConnect.Interfaces;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexBrokerageTests
    {
        private BitfinexBrokerage _unit;
        private Mock<IRestClient> _rest;
        private readonly Mock<IWebSocket> _wss = new Mock<IWebSocket>();
        private Symbol _symbol;
        private readonly Mock<IAlgorithm> _algo = new Mock<IAlgorithm>();
        private readonly AccountType _accountType = AccountType.Margin;

        [SetUp]
        public void Setup()
        {
            _rest = new Mock<IRestClient>();
            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(_accountType);

            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);
            _symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);

            //todo: test data
            var setupData = new Dictionary<string, string>
            {
                { Constants.NewOrderRequestUrl, "bitfinex_order.json" },
                { Constants.OrderCancelRequestUrl, "bitfinex_cancel.json" },
                { Constants.PubTickerRequestUrl + "/btcusd", "bitfinex_ticker.json" },
                { Constants.PubTickerRequestUrl + "/ethbtc", "bitfinex_ticker_ethbtc.json" },
                { Constants.PubTickerRequestUrl + "/ethusd", "bitfinex_ticker_ethusd.json" },
                { Constants.ActiveOrdersRequestUrl, "bitfinex_open.json" },
                { Constants.ActivePositionsRequestUrl, "bitfinex_position.json" },
                { Constants.BalanceRequestUrl, "bitfinex_wallet.json" }
            };

            _rest.Setup(m => m.Execute(It.IsAny<IRestRequest>())).Returns<RestRequest>(r => new RestResponse
            {
                Content = File.ReadAllText("TestData//" + setupData[r.Resource]),
                StatusCode = HttpStatusCode.OK
            });
        }

        [Test]
        public void PlaceOrderSubmittedTest()
        {
            var response = new PlaceOrderResponse
            {
                OrderId = 1
            };

            _algo.Setup(a => a.Securities).Returns(BitfinexTestsHelpers.CreateHoldings(0));

            var raised = new ManualResetEvent(false);
            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Submitted, e.Status);
                Assert.AreEqual(0, e.FillQuantity);
                raised.Set();
            };
            var actual = _unit.PlaceOrder(new Orders.MarketOrder(_symbol, 100, DateTime.UtcNow));

            Assert.IsTrue(actual);
            Assert.IsTrue(raised.WaitOne(100));
        }

        [Test]
        public void UpdateOrderTest()
        {
            var brokerId = 123;
            var response = new OrderStatusResponse
            {
                Id = brokerId,
                Symbol = "BTCUSD"
            };

            var placed = new PlaceOrderResponse
            {
                OrderId = 1,
                Symbol = "BTCUSD"
            };

            var isCancel = true;
            var cancel = new ManualResetEvent(false);
            var open = new ManualResetEvent(false);

            _unit.OrderStatusChanged += (s, e) =>
            {
                if (isCancel)
                {
                    Assert.AreEqual(0, e.OrderFee);
                    Assert.AreEqual(Orders.OrderStatus.Canceled, e.Status);
                    isCancel = false;
                    cancel.Set();
                    return;
                }
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Submitted, e.Status);
                open.Set();
            };

            var actual = _unit.UpdateOrder(new Orders.MarketOrder { BrokerId = new List<string> { brokerId.ToString() }, Symbol = _symbol });
            Assert.IsTrue(actual);
            Assert.IsTrue(cancel.WaitOne(1000));
            Assert.IsTrue(open.WaitOne(1000));
        }

        [Test]
        public void CancelOrderTest()
        {
            var brokerId = 123;

            var raised = new ManualResetEvent(false);
            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Canceled, e.Status);
                raised.Set();
            };

            var actual = _unit.CancelOrder(new Orders.MarketOrder(_symbol, 100, DateTime.UtcNow) { BrokerId = new List<string> { brokerId.ToString() } });

            Assert.IsTrue(actual);
        }

        [Test]
        public void GetOpenOrdersTest()
        {
            _unit.CachedOrderIDs.TryAdd(1, new Orders.MarketOrder { BrokerId = new List<string> { "448411366" }, Price = 123 });

            var actual = _unit.GetOpenOrders();

            Assert.AreEqual(0.02m, actual.First().Quantity);
            Assert.AreEqual(10000m, actual.First().Price);
            Assert.AreEqual("BTCUSD", actual.First().Symbol.Value);
            Assert.AreEqual(Orders.OrderDirection.Buy, actual.First().Direction);
            Assert.AreEqual(Orders.OrderType.Limit, actual.First().Type);
            Assert.AreEqual(Orders.OrderStatus.Submitted, actual.First().Status);

            Assert.AreEqual(Orders.OrderStatus.PartiallyFilled, actual.Last().Status);
            Assert.AreEqual(0.01m, actual.Last().Quantity);
            Assert.AreEqual(1, actual.Last().Id);

            Assert.AreEqual(2, actual.Count());
            Assert.That(_unit.CachedOrderIDs.Count(b => b.Value.BrokerId.Contains("448411366")) == 1);
            Assert.AreEqual(1, _unit.CachedOrderIDs.Count());
        }

        [Test]
        public void GetAccountHoldingsTest()
        {
            var actual = _unit.GetAccountHoldings();

            Assert.AreEqual(0, actual.Count());
        }

        [Test]
        public void GetCashBalanceTest()
        {
            var actual = _unit.GetCashBalance();

            Assert.AreEqual(1, actual.Where(e => e.Symbol == "USD").Single().Amount);
            Assert.AreEqual(1, actual.Where(e => e.Symbol == "USD").Single().ConversionRate);

            Assert.AreEqual(2, actual.Where(e => e.Symbol == "BTC").Single().Amount);
            Assert.AreEqual(244.755m, actual.Where(e => e.Symbol == "BTC").Single().ConversionRate);
        }

        [Test]
        public void PlaceOrderCrossesZeroSubmittedTest()
        {
            _algo.Setup(a => a.Securities).Returns(BitfinexTestsHelpers.CreateHoldings(200m));

            var quantity = -200;
            var raised = new ManualResetEvent(false);

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Submitted, e.Status);
                Assert.AreEqual(0, e.FillQuantity);
                raised.Set();
            };
            var actual = _unit.PlaceOrder(new Orders.MarketOrder(_symbol, quantity, DateTime.UtcNow) { Id = 123 });

            Assert.IsTrue(actual);

            Assert.IsTrue(raised.WaitOne(100));
            _unit.CachedOrderIDs[123].BrokerId.Count();
        }
    }
}