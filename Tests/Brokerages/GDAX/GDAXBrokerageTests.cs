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

using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.GDAX;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using QuantConnect.Util;
using Order = QuantConnect.Orders.Order;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.GDAX
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class GDAXBrokerageTests
    {
        #region Declarations

        private GDAXFakeDataQueueHandler _unit;
        private readonly Mock<IWebSocket> _wss = new Mock<IWebSocket>();
        private readonly Mock<IRestClient> _rest = new Mock<IRestClient>();
        private readonly Mock<IAlgorithm> _algo = new Mock<IAlgorithm>();
        private string _openOrderData;
        private string _fillData;
        private string _accountsData;
        private string _holdingData;
        private string _tickerData;
        private Symbol _symbol;
        private const string BrokerId = "d0c5340b-6d6c-49d9-b567-48c4bfca13d2";
        private const string MatchBrokerId = "132fb6ae-456b-4654-b4e0-d681ac05cea1";
        private const AccountType AccountType = QuantConnect.AccountType.Margin;

        #endregion

        [SetUp]
        public void Setup()
        {
            var priceProvider = new Mock<IPriceProvider>();
            priceProvider.Setup(x => x.GetLastPrice(It.IsAny<Symbol>())).Returns(1.234m);

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource.StartsWith("/products/"))))
                .Returns(new RestResponse
                {
                    Content = File.ReadAllText("TestData//gdax_tick.txt"),
                    StatusCode = HttpStatusCode.OK
                });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/orders" && r.Method == Method.GET)))
                .Returns(new RestResponse
                {
                    Content = "[]",
                    StatusCode = HttpStatusCode.OK
                });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/orders" && r.Method == Method.POST)))
                .Returns(new RestResponse
                {
                    Content = File.ReadAllText("TestData//gdax_order.txt"),
                    StatusCode = HttpStatusCode.OK
                });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource.StartsWith("/orders/" + BrokerId) || r.Resource.StartsWith("/orders/" + MatchBrokerId))))
                .Returns(new RestResponse
                {
                    Content = File.ReadAllText("TestData//gdax_orderById.txt"),
                    StatusCode = HttpStatusCode.OK
                });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/fills")))
                .Returns(new RestResponse
                {
                    Content = "[]",
                    StatusCode = HttpStatusCode.OK
                });

            _unit = new GDAXFakeDataQueueHandler("wss://localhost", _wss.Object, _rest.Object, "abc", "MTIz", "pass", _algo.Object, priceProvider.Object, new AggregationManager());

            _fillData = File.ReadAllText("TestData//gdax_fill.txt");
            _openOrderData = File.ReadAllText("TestData//gdax_openOrders.txt");
            _accountsData = File.ReadAllText("TestData//gdax_accounts.txt");
            _holdingData = File.ReadAllText("TestData//gdax_holding.txt");
            _tickerData = File.ReadAllText("TestData//gdax_ticker.txt");

            _symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);

            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(AccountType);
            _algo.Setup(a => a.AccountCurrency).Returns(Currencies.USD);
        }

        [TearDown]
        public void TearDown()
        {
            _unit.Disconnect();
            _unit.DisposeSafely();
        }

        private void SetupResponse(string body, HttpStatusCode httpStatus = HttpStatusCode.OK)
        {
            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => !r.Resource.StartsWith("/products/") && !r.Resource.StartsWith("/orders/" + BrokerId))))
                .Returns(new RestResponse
                {
                    Content = body,
                    StatusCode = httpStatus
                });
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
        public void ConnectTest()
        {
            _wss.Setup(m => m.Connect()).Raises(m => m.Open += null, EventArgs.Empty).Verifiable();
            _wss.Setup(m => m.IsOpen).Returns(false);
            _unit.Connect();
            _wss.Verify();
        }

        [Test]
        public void DisconnectTest()
        {
            _wss.Setup(m => m.Close()).Verifiable();
            _wss.Setup(m => m.IsOpen).Returns(true);
            _unit.Disconnect();
            _wss.Verify();
        }

        [Test]
        public void OnOrderFillTest()
        {
            const decimal orderQuantity = 6.1m;

            _unit.PlaceOrder(new MarketOrder("BTCUSD", orderQuantity, DateTime.UtcNow)
            {
                // set the quote currency here to prevent the test from accessing algorithm.Securities
                PriceCurrency = "USD"
            });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource == "/fills")))
                .Returns(new RestResponse
                {
                    Content = _fillData,
                    StatusCode = HttpStatusCode.OK
                });

            var raised = new ManualResetEvent(false);

            var isFilled = false;
            var actualFee = 0m;
            var actualQuantity = 0m;

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                actualFee += e.OrderFee.Value.Amount;
                Assert.AreEqual(Currencies.USD, e.OrderFee.Value.Currency);
                actualQuantity += e.AbsoluteFillQuantity;

                Assert.IsTrue(actualQuantity != orderQuantity);
                Assert.AreEqual(OrderStatus.PartiallyFilled, e.Status);
                Assert.AreEqual(5.23512, e.FillQuantity);
                Assert.AreEqual(12, actualFee);

                isFilled = true;

                raised.Set();
            };

            raised.WaitOne(3000);
            raised.DisposeSafely();

            Assert.IsTrue(isFilled);
        }

        [Test]
        public void GetAuthenticationTokenTest()
        {
            var actual = _unit.GetAuthenticationToken("", "POST", "http://localhost");

            Assert.IsFalse(string.IsNullOrEmpty(actual.Signature));
            Assert.IsFalse(string.IsNullOrEmpty(actual.Timestamp));
            Assert.AreEqual("pass", actual.Passphrase);
            Assert.AreEqual("abc", actual.Key);
        }

        [TestCase("1", HttpStatusCode.OK, OrderStatus.Submitted, 1.23, 0, OrderType.Market)]
        [TestCase("1", HttpStatusCode.OK, OrderStatus.Submitted, -1.23, 0, OrderType.Market)]
        [TestCase("1", HttpStatusCode.OK, OrderStatus.Submitted, 1.23, 1234.56, OrderType.Limit)]
        [TestCase("1", HttpStatusCode.OK, OrderStatus.Submitted, -1.23, 1234.56, OrderType.Limit)]
        [TestCase("1", HttpStatusCode.OK, OrderStatus.Submitted, 1.23, 1234.56, OrderType.StopMarket)]
        [TestCase("1", HttpStatusCode.OK, OrderStatus.Submitted, -1.23, 1234.56, OrderType.StopMarket)]
        [TestCase(null, HttpStatusCode.BadRequest, OrderStatus.Invalid, 1.23, 1234.56, OrderType.Market)]
        [TestCase(null, HttpStatusCode.BadRequest, OrderStatus.Invalid, 1.23, 1234.56, OrderType.Limit)]
        [TestCase(null, HttpStatusCode.BadRequest, OrderStatus.Invalid, 1.23, 1234.56, OrderType.StopMarket)]
        public void PlaceOrderTest(string orderId, HttpStatusCode httpStatus, OrderStatus status, decimal quantity, decimal price, OrderType orderType)
        {
            var response = new
            {
                id = BrokerId,
                fill_fees = "0.11"
            };
            SetupResponse(JsonConvert.SerializeObject(response), httpStatus);

            _unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual(status, e.Status);
                if (orderId != null)
                {
                    Assert.AreEqual("BTCUSD", e.Symbol.Value);
                    Assert.That((quantity > 0 && e.Direction == OrderDirection.Buy) || (quantity < 0 && e.Direction == OrderDirection.Sell));
                    Assert.IsTrue(orderId == null || _unit.CachedOrderIDs.SelectMany(c => c.Value.BrokerId.Where(b => b == BrokerId)).Any());
                }
            };

            Order order;
            if (orderType == OrderType.Limit)
            {
                order = new LimitOrder(_symbol, quantity, price, DateTime.UtcNow);
            }
            else if (orderType == OrderType.Market)
            {
                order = new MarketOrder(_symbol, quantity, DateTime.UtcNow);
            }
            else
            {
                order = new StopMarketOrder(_symbol, quantity, price, DateTime.UtcNow);
            }

            var actual = _unit.PlaceOrder(order);

            Assert.IsTrue(actual || (orderId == null && !actual));
        }

        [Test]
        public void GetOpenOrdersTest()
        {
            SetupResponse(_openOrderData);

            _unit.CachedOrderIDs.TryAdd(1, new MarketOrder { BrokerId = new List<string> { "1" }, Price = 123 });

            var actual = _unit.GetOpenOrders();

            Assert.AreEqual(2, actual.Count);
            Assert.AreEqual(0.01, actual.First().Quantity);
            Assert.AreEqual(OrderDirection.Buy, actual.First().Direction);
            Assert.AreEqual(0.1, actual.First().Price);

            Assert.AreEqual(-1, actual.Last().Quantity);
            Assert.AreEqual(OrderDirection.Sell, actual.Last().Direction);
            Assert.AreEqual(1, actual.Last().Price);

        }

        [Test]
        public void GetTickTest()
        {
            var actual = _unit.GetTick(_symbol);
            Assert.AreEqual(333.98m, actual.BidPrice);
            Assert.AreEqual(333.99m, actual.AskPrice);
            Assert.AreEqual(5957.11914015, actual.Quantity);
        }

        [Test]
        public void GetCashBalanceTest()
        {
            SetupResponse(_accountsData);

            var actual = _unit.GetCashBalance();

            Assert.AreEqual(2, actual.Count);

            var usd = actual.Single(a => a.Currency == Currencies.USD);
            var btc = actual.Single(a => a.Currency == "BTC");

            Assert.AreEqual(80.2301373066930000m, usd.Amount);
            Assert.AreEqual(1.1, btc.Amount);
        }

        [Test, Ignore("Holdings are now set to 0 swaps at the start of each launch. Not meaningful.")]
        public void GetAccountHoldingsTest()
        {
            SetupResponse(_holdingData);

            _unit.CachedOrderIDs.TryAdd(1, new MarketOrder { BrokerId = new List<string> { "1" }, Price = 123 });

            var actual = _unit.GetAccountHoldings();

            Assert.AreEqual(0, actual.Count);
        }

        [TestCase(HttpStatusCode.OK, HttpStatusCode.NotFound, false)]
        [TestCase(HttpStatusCode.OK, HttpStatusCode.OK, true)]
        public void CancelOrderTest(HttpStatusCode code, HttpStatusCode code2, bool expected)
        {
            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => !r.Resource.EndsWith("1"))))
                .Returns(new RestResponse
                {
                    StatusCode = code
                });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => !r.Resource.EndsWith("2"))))
                .Returns(new RestResponse
                {
                    StatusCode = code2
                });

            var actual = _unit.CancelOrder(new LimitOrder { BrokerId = new List<string> { "1", "2" } });

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UpdateOrderTest()
        {
            Assert.Throws<NotSupportedException>(() => _unit.UpdateOrder(new LimitOrder()));
        }

        [Test]
        public void SubscribeTest()
        {
            string actual = null;

            _wss.Setup(w => w.Send(It.IsAny<string>())).Callback<string>(c => actual = c);

            var gotBTCUSD = false;
            var gotGBPUSD = false;
            var gotBTCETH = false;

            _unit.Subscribe(GetSubscriptionDataConfig<Tick>(Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX), Resolution.Tick), (s, e) => { gotBTCUSD = true; });
            StringAssert.Contains("[\"BTC-USD\"]", actual);
            _unit.Subscribe(GetSubscriptionDataConfig<Tick>(Symbol.Create("GBPUSD", SecurityType.Forex, Market.FXCM), Resolution.Tick), (s, e) => { gotGBPUSD = true; });
            _unit.Subscribe(GetSubscriptionDataConfig<Tick>(Symbol.Create("BTCETH", SecurityType.Crypto, Market.GDAX), Resolution.Tick), (s, e) => { gotBTCETH = true; });
            StringAssert.Contains("[\"BTC-USD\",\"BTC-ETH\"]", actual);
            Thread.Sleep(1000);

            Assert.IsFalse(gotBTCUSD);
            Assert.IsTrue(gotGBPUSD);
            Assert.IsFalse(gotBTCETH);
        }

        [Test]
        public void UnsubscribeTest()
        {
            string actual = null;
            _wss.Setup(w => w.IsOpen).Returns(true);
            _wss.Setup(w => w.Send(It.IsAny<string>())).Callback<string>(c => actual = c);
            _unit.Unsubscribe(new List<Symbol> { Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX) });
            StringAssert.Contains("user", actual);
            StringAssert.Contains("heartbeat", actual);
            StringAssert.DoesNotContain("matches", actual);
        }

        [Test]
        public void PollTickTest()
        {
            var gotGBPUSD = false;
            var enumerator = _unit.Subscribe(GetSubscriptionDataConfig<Tick>(Symbol.Create("GBPUSD", SecurityType.Forex, Market.FXCM), Resolution.Tick), (s, e) => { gotGBPUSD = true; });
            Thread.Sleep(1000);

            // conversion rate is the price returned by the QC pricing API
            Assert.IsTrue(gotGBPUSD);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1.234m, enumerator.Current.Price);
        }

        [Test]
        public void ErrorTest()
        {
            string actual = null;

            // subscribe to invalid symbol
            const string expected = "[\"BTC-LTC\"]";
            _wss.Setup(w => w.Send(It.IsAny<string>())).Callback<string>(c => actual = c);

            _unit.Subscribe(new[] { Symbol.Create("BTCLTC", SecurityType.Crypto, Market.GDAX) });

            StringAssert.Contains(expected, actual);

            BrokerageMessageType messageType = 0;
            _unit.Message += (sender, e) => { messageType = e.Type; };
            const string json = "{\"type\":\"error\",\"message\":\"Failed to subscribe\",\"reason\":\"Invalid product ID provided\"}";
            _unit.OnMessage(_unit, GDAXTestsHelpers.GetArgs(json));

            Assert.AreEqual(BrokerageMessageType.Error, messageType);
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        private class GDAXFakeDataQueueHandler : GDAXDataQueueHandler
        {
            protected override string[] ChannelNames => new[] { "heartbeat", "user" };

            public GDAXFakeDataQueueHandler(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string passPhrase, IAlgorithm algorithm,
                IPriceProvider priceProvider, IDataAggregator aggregator)
            : base(wssUrl, websocket, restClient, apiKey, apiSecret, passPhrase, algorithm, priceProvider, aggregator)
            {
            }
        }
    }
}
