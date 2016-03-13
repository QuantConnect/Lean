using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Orders.Fees;
using NUnit.Framework;
using QuantConnect.Orders;
using QuantConnect.Securities;
using Moq;
using NodaTime;
using QuantConnect.Data;
using System.Reflection;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture()]
    public class BitfinexFeeModelTests
    {

        BitfinexFeeModel unit;
        Symbol symbol;
        SubscriptionDataConfig sub;
        decimal price = 100.00m;
        Mock<Security> security;

        [SetUp]
        public void Setup()
        {
            unit = new BitfinexFeeModel();
            symbol = Symbol.Create("BTCUSD", SecurityType.Forex, Market.Bitfinex);

            //hack: no parameterless constructors
            sub = (SubscriptionDataConfig)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SubscriptionDataConfig));
            security = new Mock<Security>(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc), sub, new Cash("USD",1,1), new SymbolProperties("","USD",1,1));
            security.Setup(s => s.Price).Returns(price);
        }

        [Test()]
        public void GetTakerOrderFeeTest()
        {          
            var order = new Mock<MarketOrder>();
            order.Setup(o => o.Type).Returns(OrderType.Market);
            order.Object.Quantity = 10;

            decimal expected = 2m;
            var actual = unit.GetOrderFee(security.Object, order.Object);

            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void GetMakerOrderFeeTest()
        {
            var order = new Mock<LimitOrder>();
            order.Setup(o => o.Type).Returns(OrderType.Limit);
            order.Object.Quantity = 10;

            decimal expected = 1m;
            var actual = unit.GetOrderFee(security.Object, order.Object);

            Assert.AreEqual(expected, actual);
        }

    }
}
