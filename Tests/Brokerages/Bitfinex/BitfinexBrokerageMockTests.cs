using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Bitfinex;
using NUnit.Framework;
using Moq;
using QuantConnect.Configuration;
using TradingApi.ModelObjects.Bitfinex.Json;
using TradingApi.ModelObjects;
namespace QuantConnect.Brokerages.Bitfinex.Tests
{
    [TestFixture()]
    public class BitfinexBrokerageMockTests
    {

        BitfinexBrokerage unit;
        Mock<TradingApi.Bitfinex.BitfinexApi> mock = new Mock<TradingApi.Bitfinex.BitfinexApi>(It.IsAny<string>(), It.IsAny<string>());
        protected Symbol Symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex);

        [SetUp()]
        public void Setup()
        {
            //DI would be preferable here           
            unit = new BitfinexBrokerage("abc", "123", "trading", mock.Object);
        }

        [Test()]
        public void PlaceOrderTest()
        {
            var response = new BitfinexNewOrderResponse
            {
                OrderId = 1,
            };
            mock.Setup(m => m.SendOrder(It.IsAny<BitfinexNewOrderPost>())).Returns(response);

            bool actual = unit.PlaceOrder(new Orders.MarketOrder(Symbol, 100, DateTime.UtcNow));

            Assert.IsTrue(actual);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Submitted, e.Status);
            };

            response.OrderId = 0;
            actual = unit.PlaceOrder(new Orders.MarketOrder(Symbol, 100, DateTime.UtcNow));
            Assert.IsFalse(actual);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Invalid, e.Status);
            };

        }

        [Test()]
        public void UpdateOrderTest()
        {
            var response = new BitfinexCancelReplaceOrderResponse { OrderId = 1 };
            mock.Setup(m => m.CancelReplaceOrder(It.IsAny<BitfinexCancelReplacePost>())).Returns(response);
            bool actual = unit.UpdateOrder(new Orders.MarketOrder { BrokerId = new List<string> { "1", "2", "3" } });
            Assert.IsTrue(actual);

            response.OrderId = 0;
            mock.Setup(m => m.CancelReplaceOrder(It.IsAny<BitfinexCancelReplacePost>())).Returns(response);
            actual = unit.UpdateOrder(new Orders.MarketOrder { BrokerId = new List<string> { "1", "2", "3" } });
            Assert.IsFalse(actual);

            response = new BitfinexCancelReplaceOrderResponse { OrderId = 2 };
            mock.Setup(m => m.CancelReplaceOrder(It.IsAny<BitfinexCancelReplacePost>())).Callback(() => { response.OrderId -= 1; }).Returns(response);
            actual = unit.UpdateOrder(new Orders.MarketOrder { BrokerId = new List<string> { "1", "2", "3" } });
            Assert.IsFalse(actual);

        }

        [Test()]
        public void CancelOrderTest()
        {
            int brokerId = 123;
            var response = new BitfinexOrderStatusResponse
            {
                Id = brokerId
            };
            mock.Setup(m => m.CancelOrder(brokerId)).Returns(response);

            bool actual = unit.CancelOrder(new Orders.MarketOrder(Symbol, 100, DateTime.UtcNow) { BrokerId = new List<string> { brokerId.ToString() } });

            Assert.IsTrue(actual);

            unit.OrderStatusChanged += (s, e) =>
            {
                Assert.AreEqual("BTCUSD", e.Symbol.Value);
                Assert.AreEqual(0, e.OrderFee);
                Assert.AreEqual(Orders.OrderStatus.Canceled, e.Status);
            };

            brokerId = 0;
            actual = unit.CancelOrder(new Orders.MarketOrder(Symbol, 100, DateTime.UtcNow) { BrokerId = new List<string> { brokerId.ToString() } });
            Assert.IsFalse(actual);
        }

        [Test()]
        public void GetOpenOrdersTest()
        {
            decimal expected = 456m;
            var list = new BitfinexOrderStatusResponse[] {
                new BitfinexOrderStatusResponse
                {
                    Id = 1,
                    OriginalAmount ="1",
                    Timestamp = "1",
                    Price = expected.ToString(),
                    RemainingAmount = "1",
                    ExecutedAmount = "0",
                    IsLive = true,
                    IsCancelled = false
                }
            };
            mock.Setup(m => m.GetActiveOrders()).Returns(list);
            unit.CachedOrderIDs.TryAdd(1, new Orders.MarketOrder { BrokerId = new List<string> { "1" }, Price = 123 });

            var actual = unit.GetOpenOrders();

            Assert.AreEqual(1, actual.Count());
            Assert.AreEqual(expected / 100, unit.CachedOrderIDs.First().Value.Price);

            list.First().Id = 123;
            actual = unit.GetOpenOrders();
            Assert.AreEqual(1, actual.Count());

            list.First().Id = 1;
            list.First().ExecutedAmount = "1";
            list.First().RemainingAmount = "0";
            actual = unit.GetOpenOrders();
            Assert.AreEqual(1, actual.Count());
        }

        [Test()]
        public void GetAccountHoldingsTest()
        {

        }

        [Test()]
        public void GetCashBalanceTest()
        {

        }

        [Test()]
        public void SubscribeTest()
        {
            unit.Connect();
            var response = new BitfinexPublicTickerGet
            {
                Ask = "1",
                Bid = "2",
                High = "3",
                LastPrice = "4",
                Low = "5",
                Mid = "6",
                Timestamp = "7",
                Volume = "8"
            };
            mock.Setup(m => m.GetPublicTicker(BtcInfo.PairTypeEnum.btcusd, BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker)).Returns(response);
            unit.Subscribe(null, null);
            System.Threading.Thread.Sleep(9000);
            var actual = unit.GetNextTicks();
            unit.Unsubscribe(null, null);
            Assert.AreEqual(2, actual.Count());
        }

    }
}
