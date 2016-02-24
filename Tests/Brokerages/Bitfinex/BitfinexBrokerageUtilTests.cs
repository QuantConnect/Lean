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
using TradingApi.ModelObjects.Bitfinex.Json;
using QuantConnect.Orders;
using System.Reflection;
using Moq;


namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    [TestFixture]
    public class BitfinexBrokerageUtilTests
    {

        BitfinexBrokerage unit;

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

        [SetUp]
        public void Setup()
        {
            Config.Set("bitfinex-api-secret", "abc");
            Config.Set("bitfinex-api-key", "123");
            unit = new BitfinexWebsocketsBrokerage();
        }

        [Test()]
        public void MapOrderStatusTest()
        {
            BitfinexOrderStatusResponse response = new BitfinexOrderStatusResponse
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
        public void MapOrderTypeTest()
        {

            Config.Set("bitfinex-wallet", "exchange");
            unit = new BitfinexBrokerage();

            var expected = GetDescriptionFromEnumValue(BitfinexOrderType.exchangeMarket);
            var actual = unit.MapOrderType(OrderType.Market);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.exchangeLimit);
            actual = unit.MapOrderType(OrderType.Limit);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.exchangeStop);
            actual = unit.MapOrderType(OrderType.StopMarket);
            Assert.AreEqual(expected, actual);

            Assert.Throws<Exception>(() => unit.MapOrderType(OrderType.StopLimit));

            Config.Set("bitfinex-wallet", "trading");
            unit = new BitfinexBrokerage();

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.market);
            actual = unit.MapOrderType(OrderType.Market);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.limit);
            actual = unit.MapOrderType(OrderType.Limit);
            Assert.AreEqual(expected, actual);

            expected = GetDescriptionFromEnumValue(BitfinexOrderType.stop);
            actual = unit.MapOrderType(OrderType.StopMarket);
            Assert.AreEqual(expected, actual);

        }

        [Test()]
        public void MapOrderTypeTest1()
        {

            Config.Set("bitfinex-wallet", "exchange");
            unit = new BitfinexBrokerage();

            var expected = OrderType.Market;
            var actual = unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeMarket));
            Assert.AreEqual(expected, actual);

            expected = OrderType.Limit;
            actual = unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeLimit));
            Assert.AreEqual(expected, actual);

            expected = OrderType.StopMarket;
            actual = unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeStop));
            Assert.AreEqual(expected, actual);

            Assert.Throws<Exception>(() => unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeTrailingStop)));

            Assert.Throws<Exception>(() => unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.market)));

            Config.Set("bitfinex-wallet", "trading");
            unit = new BitfinexBrokerage();

            expected = OrderType.Market;
            actual = unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.market));
            Assert.AreEqual(expected, actual);

            expected = OrderType.Limit;
            actual = unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.limit));
            Assert.AreEqual(expected, actual);

            expected = OrderType.StopMarket;
            actual = unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.stop));
            Assert.AreEqual(expected, actual);

            Assert.Throws<Exception>(() => unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeMarket)));
            Assert.Throws<Exception>(() => unit.MapOrderType(GetDescriptionFromEnumValue(BitfinexOrderType.exchangeLimit)));

        }

        [Test()]
        public void MapOrderStatusTest1()
        {
            var msg = new TradeMessage(new string[] { "<TRD_SEQ>", "<TRD_ID>", "<TRD_PAIR>", "1", "2", "3", "4", "<ORD_TYPE>", "5", "0", "<FEE_CURRENCY>" });
            var actual = BitfinexBrokerage.MapOrderStatus(msg);
            Assert.AreEqual(OrderStatus.PartiallyFilled, actual);

            msg.FEE = 1m;
            actual = BitfinexBrokerage.MapOrderStatus(msg);
            Assert.AreEqual(OrderStatus.Filled, actual);
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
