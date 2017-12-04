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
using Moq;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Brokerages.Bitfinex.Rest;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using RestSharp;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexBrokerageUtilityTests
    {
        private BitfinexBrokerage _unit;
        private Mock<IRestClient> _rest = new Mock<IRestClient>();
        private readonly Mock<IWebSocket> _wss = new Mock<IWebSocket>();
        private Symbol _symbol;
        private Mock<IAlgorithm> _algo = new Mock<IAlgorithm>();
        private readonly AccountType _accountType = AccountType.Margin;

        private enum BitfinexOrderType
        {
            [System.ComponentModel.Description("exchange market")] exchangeMarket,
            [System.ComponentModel.Description("exchange limit")] exchangeLimit,
            [System.ComponentModel.Description("exchange stop")] exchangeStop,
            [System.ComponentModel.Description("exchange trailing stop")] exchangeTrailingStop,
            [System.ComponentModel.Description("market")] market,
            [System.ComponentModel.Description("limit")] limit,
            [System.ComponentModel.Description("stop")] stop,
            [System.ComponentModel.Description("trailing stop")] trailingStop
        }

        [SetUp]
        public void Setup()
        {
            _algo = new Mock<IAlgorithm>();
            _algo.Setup(a => a.BrokerageModel.AccountType).Returns(_accountType);
            _unit = new BitfinexBrokerage("http://localhost", _wss.Object, _rest.Object, "abc", "123", _algo.Object);
            _rest = new Mock<IRestClient>();
            _symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);
        }

        [Test]
        public void MapOrderStatusTest()
        {
            var response = new OrderStatusResponse
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

        [Test]
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

        [Test]
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
            var attribute = value.GetType()
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
            var fields = type.GetFields();
            var field = fields
                .SelectMany(f => f.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false), (f, a) => new { Field = f, Att = a })
                .Where(a => ((System.ComponentModel.DescriptionAttribute)a.Att).Description == description).SingleOrDefault();
            return field == null ? default(T) : (T)field.Field.GetRawConstantValue();
        }
    }
}