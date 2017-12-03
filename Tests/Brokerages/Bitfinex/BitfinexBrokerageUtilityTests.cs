using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Bitfinex;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using System.Reflection;
using Moq;
using QuantConnect.Brokerages.Bitfinex.Rest;
using RestSharp;
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexBrokerageUtilityTests
    {

        BitfinexBrokerage _unit;
        Mock<IRestClient> _rest = new Mock<IRestClient>();
        Mock<IWebSocket> _wss = new Mock<IWebSocket>();
        Symbol _symbol;
        Mock<IAlgorithm> _algo = new Mock<IAlgorithm>();
        AccountType _accountType = AccountType.Margin;

        private enum BitfinexOrderType
        {
            [System.ComponentModel.Description("exchange market")]
            exchangeMarket,
            [System.ComponentModel.Description("exchange limit")]
            exchangeLimit,
            [System.ComponentModel.Description("exchange stop")]
            exchangeStop,
            [System.ComponentModel.Description("exchange trailing stop")]
            exchangeTrailingStop,
            [System.ComponentModel.Description("market")]
            market,
            [System.ComponentModel.Description("limit")]
            limit,
            [System.ComponentModel.Description("stop")]
            stop,
            [System.ComponentModel.Description("trailing stop")]
            trailingStop
        }

        [SetUp()]
        public void Setup()
        {
            _algo = new Mock<IAlgorithm>();
            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(_accountType);
            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);
            _rest = new Mock<IRestClient>();
            _symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);
        }

        [Test()]
        public void MapOrderStatusTest()
        {
            OrderStatusResponse response = new OrderStatusResponse
            {
                IsCancelled = true,
                IsLive = true
            };

            var expected = OrderStatus.Canceled;
            var actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.IsCancelled = false;
            expected = OrderStatus.Submitted;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.RemainingAmount = "1";
            response.ExecutedAmount = "0";
            expected = OrderStatus.Submitted;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.RemainingAmount = "1";
            response.ExecutedAmount = "1";
            expected = OrderStatus.PartiallyFilled;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.RemainingAmount = "0";
            response.ExecutedAmount = "1";
            expected = OrderStatus.Submitted;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.RemainingAmount = "0";
            response.ExecutedAmount = "0";
            response.IsLive = false;
            expected = OrderStatus.Invalid;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.RemainingAmount = "";
            response.ExecutedAmount = "";
            response.IsLive = false;
            expected = OrderStatus.Invalid;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);

            response.RemainingAmount = null;
            response.ExecutedAmount = null;
            response.IsLive = false;
            expected = OrderStatus.Invalid;
            actual = BitfinexBrokerage.MapOrderStatus(response);
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void MapOrderTypeToBitfinexTest()
        {
            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(AccountType.Cash);
            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);

            var expected = GetDescriptionFromEnumValue(BitfinexOrderType.exchangeMarket);
            var actual = _unit.MapOrderType(OrderType.Market);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.exchangeLimit);
            actual = _unit.MapOrderType(OrderType.Limit);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.exchangeStop);
            actual = _unit.MapOrderType(OrderType.StopMarket);
            Assert.AreEqual(expected, actual);

            Assert.Throws<Exception>(() => _unit.MapOrderType(OrderType.StopLimit));

            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(AccountType.Margin);
            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.market);
            actual = _unit.MapOrderType(OrderType.Market);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.limit);
            actual = _unit.MapOrderType(OrderType.Limit);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.stop);
            actual = _unit.MapOrderType(OrderType.StopMarket);
            Assert.AreEqual(expected, actual);

        }

        [Test()]
        public void MapOrderTypeFromBitfinexTest()
        {

            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(AccountType.Cash);
            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);

            var expected = OrderType.Market;
            var actual = _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeMarket));
            Assert.AreEqual(expected, actual);

            expected = OrderType.Limit;
            actual = _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeLimit));
            Assert.AreEqual(expected, actual);

            expected = OrderType.StopMarket;
            actual = _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeStop));
            Assert.AreEqual(expected, actual);

            Assert.Throws<Exception>(() => _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeTrailingStop)));

            Assert.Throws<Exception>(() => _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.market)));

            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(AccountType.Margin);
            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);

            expected = OrderType.Market;
            actual = _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.market));
            Assert.AreEqual(expected, actual);

            expected = OrderType.Limit;
            actual = _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.limit));
            Assert.AreEqual(expected, actual);

            expected = OrderType.StopMarket;
            actual = _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.stop));
            Assert.AreEqual(expected, actual);

            Assert.Throws<Exception>(() => _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeMarket)));
            Assert.Throws<Exception>(() => _unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeLimit)));

        }

        private static string GetDescriptionFromEnumValue(Enum value)
        {
            System.ComponentModel.DescriptionAttribute attribute = value.GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .SingleOrDefault() as System.ComponentModel.DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        private static T GetEnumValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new ArgumentException();
            FieldInfo[] fields = type.GetFields();
            var field = fields
                .SelectMany(f => f.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false), (f, a) => new { Field = f, Att = a })
                .Where(a => ((System.ComponentModel.DescriptionAttribute)a.Att).Description == description).SingleOrDefault();
            return field == null ? default(T) : (T)field.Field.GetRawConstantValue();
        }



    }
}
