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
using System.Linq;
using NUnit.Framework;
using WebSocketSharp;
using Moq;
using System.Threading;
using System.IO;
using QuantConnect.Tests.Brokerages.GDAX;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using QuantConnect.Api;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Brokerages.GDAX;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages.GDAX
{

    [TestFixture()]
    public class GDAXBrokerageTests
    {

        GDAXBrokerage unit;
        Mock<IWebSocket> _wss = new Mock<IWebSocket>();
        Mock<IRestClient> _rest = new Mock<IRestClient>();
        string _orderData;
        string _openOrderData;
        string _matchData;
        string _accountsData;
        string _holdingData;
        Symbol _symbol;

        [SetUp()]
        public void Setup()
        {
            unit = new GDAXBrokerage("wss://localhost", _wss.Object, _rest.Object, "abc", "MTIz", "pass", "123");
            _orderData = File.ReadAllText("TestData\\gdax_order.txt");
            _matchData = File.ReadAllText("TestData\\gdax_match.txt");
            _openOrderData = File.ReadAllText("TestData\\gdax_openOrders.txt");
            _accountsData = File.ReadAllText("TestData\\gdax_accounts.txt");
            _holdingData = File.ReadAllText("TestData\\gdax_holding.txt");

            _symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.GDAX);

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => r.Resource.StartsWith("/products/")))).Returns(new RestSharp.RestResponse
            {
                Content = File.ReadAllText("TestData\\gdax_tick.txt"),
                StatusCode = HttpStatusCode.OK
            });
        }

        private void SetupResponse(string body, HttpStatusCode httpStatus = HttpStatusCode.OK)
        {
            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => !r.Resource.StartsWith("/products/")))).Returns(new RestSharp.RestResponse
            {
                Content = body,
                StatusCode = httpStatus
            });
        }

        [Test()]
        public void IsConnectedTest()
        {
            _wss.Setup(w => w.ReadyState).Returns(WebSocketState.Open);
            Assert.IsTrue(unit.IsConnected);
            _wss.Setup(w => w.ReadyState).Returns(WebSocketState.Closed);
            Assert.IsFalse(unit.IsConnected);
        }

        [Test()]
        public void ConnectTest()
        {
            _wss.Setup(m => m.Connect()).Verifiable();

            unit.Connect();
            _wss.Verify();
        }

        [Test()]
        public void DisconnectTest()
        {
            _wss.Setup(m => m.Close()).Verifiable();
            unit.Connect();
            unit.Disconnect();
            _wss.Verify();
        }

        [TestCase("open", "buy")]
        [TestCase("open", "sell")]
        [TestCase("change", "buy")]
        [TestCase("change", "sell")]
        public void OnMessageOrderOpenOrChangeTest(string type, string side)
        {
            string json = _orderData.Replace("type_value", type).Replace("side_value", side);
            string orderId = "d50ec984-77a8-460a-b958-66f114b0de9b";

            unit.Subscribe(null, new[] { _symbol });

            unit.AskPrices[_symbol].TryAdd(orderId, 124m);
            unit.BidPrices[_symbol].TryAdd(orderId, 122m);

            unit.OnMessage(unit, GDAXTestsHelpers.GetArgs(json));
            var actual = unit.Ticks.First();

            Assert.AreEqual(123.45m, side == "buy" ? actual.BidPrice : actual.AskPrice);
            var mid = (unit.AskPrices[_symbol].Min(a => a.Value) + unit.BidPrices[_symbol].Max(b => b.Value)) / 2m;
            Assert.AreEqual(mid, actual.Price);
            Assert.AreEqual("BTCUSD", actual.Symbol.Value.ToString());
        }

        [TestCase(5.23512)]
        [TestCase(6.1)]
        [TestCase(99)]
        public void OnMessageFillTest(decimal expectedQuantity)
        {
            string json = _matchData;
            string id = "132fb6ae-456b-4654-b4e0-d681ac05cea1";
            //not our order
            if (expectedQuantity == 99)
            {
                json = json.Replace(id, Guid.NewGuid().ToString());
            }

            decimal orderQuantity = 6.1m;
            GDAXTestsHelpers.AddOrder(unit, 1, id, orderQuantity);
            ManualResetEvent raised = new ManualResetEvent(false);

            decimal expectedFee = 0;
            decimal actualFee = 0;
            decimal actualQuantity = 0;

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                actualFee += e.OrderFee;
                actualQuantity += e.AbsoluteFillQuantity;

                Assert.AreEqual(actualQuantity != orderQuantity ? Orders.OrderStatus.PartiallyFilled : Orders.OrderStatus.Filled, e.Status);
                Assert.AreEqual(5.23512m, actualQuantity);
                //todo: fee
                Assert.AreEqual(expectedFee, Math.Round(actualFee, 8));
                raised.Set();
            };

            unit.OnMessage(unit, GDAXTestsHelpers.GetArgs(json));

            //if not our order should get no event
            Assert.AreEqual(raised.WaitOne(1000), expectedQuantity != 99);
        }

        [Test]
        public void GetAuthenticationTokenTest()
        {
            var actual = unit.GetAuthenticationToken("", "POST", "http://localhost");

            Assert.IsFalse(string.IsNullOrEmpty(actual.Signature));
            Assert.IsFalse(string.IsNullOrEmpty(actual.Timestamp));
            Assert.AreEqual("pass", actual.Passphrase);
            Assert.AreEqual("abc", actual.Key);
        }

        [TestCase("1", HttpStatusCode.OK, Orders.OrderStatus.Submitted, 1.23, 1234.56)]
        [TestCase("1", HttpStatusCode.OK, Orders.OrderStatus.Submitted, -1.23, 1234.56)]
        [TestCase(null, HttpStatusCode.BadRequest, Orders.OrderStatus.Invalid, 1.23, 1234.56)]
        public void PlaceOrderTest(string orderId, HttpStatusCode httpStatus, Orders.OrderStatus status, decimal quantity, decimal price)
        {
            var response = new
            {
                order_id = 1,
            };
            SetupResponse(JsonConvert.SerializeObject(response), httpStatus);

            ManualResetEvent raised = new ManualResetEvent(false);
            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                //todo: fee
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(status, e.Status);
                Assert.That((quantity > 0 && e.Direction == Orders.OrderDirection.Buy) || (quantity < 0 && e.Direction == Orders.OrderDirection.Sell));
                raised.Set();
            };
            bool actual = unit.PlaceOrder(new Orders.LimitOrder(_symbol, quantity, price, DateTime.UtcNow));

            Assert.IsTrue(actual || (orderId == null && !actual));
            Assert.IsTrue(raised.WaitOne(1000));
        }

        [Test()]
        public void GetOpenOrdersTest()
        {
            SetupResponse(_openOrderData);

            unit.CachedOrderIDs.TryAdd(1, new Orders.MarketOrder { BrokerId = new List<string> { "1" }, Price = 123 });

            var actual = unit.GetOpenOrders();

            Assert.AreEqual(2, actual.Count());
            Assert.AreEqual(0.01, actual.First().Quantity);
            Assert.AreEqual(OrderDirection.Buy, actual.First().Direction);
            Assert.AreEqual(0.1, actual.First().Price);

            Assert.AreEqual(-1, actual.Last().Quantity);
            Assert.AreEqual(OrderDirection.Sell, actual.Last().Direction);
            Assert.AreEqual(1, actual.Last().Price);

        }

        [Test()]
        public void GetTickTest()
        {
            var actual = unit.GetTick(_symbol);
            Assert.AreEqual(333.98m, actual.BidPrice);
            Assert.AreEqual(333.99m, actual.AskPrice);
            //todo: int conversion
            Assert.AreEqual(5957, actual.Quantity);
            //Assert.AreEqual(5957.11914015, actual.Quantity);
        }

        [Test()]
        public void GetCashBalanceTest()
        {
            SetupResponse(_accountsData);

            var actual = unit.GetCashBalance();

            Assert.AreEqual(2, actual.Count());

            var usd = actual.Single(a => a.Symbol == "USD");
            var btc = actual.Single(a => a.Symbol == "BTC");

            Assert.AreEqual(80.2301373066930000m, usd.Amount);
            Assert.AreEqual(1, usd.ConversionRate);
            Assert.AreEqual(1.1, btc.Amount);
            Assert.AreEqual(333.985m, btc.ConversionRate);
        }

        [Test()]
        public void GetAccountHoldingsTest()
        {
            SetupResponse(_holdingData);

            unit.CachedOrderIDs.TryAdd(1, new Orders.MarketOrder { BrokerId = new List<string> { "1" }, Price = 123 });

            var actual = unit.GetAccountHoldings();

            Assert.AreEqual(2, actual.Count());
            Assert.AreEqual(0.005m, actual.First().Quantity);
            Assert.AreEqual(10m, actual.First().AveragePrice);

            Assert.AreEqual(-0.5m, actual.Last().Quantity);
            Assert.AreEqual(1000m, actual.Last().AveragePrice);

        }

        [TestCase(HttpStatusCode.OK, HttpStatusCode.NotFound, false)]
        [TestCase(HttpStatusCode.OK, HttpStatusCode.OK, true)]
        public void CancelOrderTest(HttpStatusCode code, HttpStatusCode code2, bool expected)
        {
            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => !r.Resource.EndsWith("1")))).Returns(new RestSharp.RestResponse
            {
                StatusCode = code
            });

            _rest.Setup(m => m.Execute(It.Is<IRestRequest>(r => !r.Resource.EndsWith("2")))).Returns(new RestSharp.RestResponse
            {
                StatusCode = code2
            });

            var actual = unit.CancelOrder(new Orders.LimitOrder { BrokerId = new List<string> { "1", "2" } });

            Assert.AreEqual(expected, actual);
        }


        [Test]
        public void UpdateOrderTest()
        {
            Assert.Throws<NotSupportedException>(() => unit.UpdateOrder(new LimitOrder()));
        }

    }
}
