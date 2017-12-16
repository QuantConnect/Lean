using System;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public class LatestPriceFillModelTests
    {
        private TestableLatestFillModel _fillModel;

        [TestFixtureSetUp]
        public void Setup()
        {
            _fillModel = new TestableLatestFillModel();
        }

        [Test]
        public void LatestPriceFillModel_UsesLatestPrice()
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, "GDAX");
            var time = new DateTime(2017, 1, 3, 0, 0, 0);
            var nextTime = time.AddSeconds(1);

            var quote = new QuoteBar(time, symbol, new Bar(1, 1, 1, 1), 1, new Bar(2, 2, 2, 2), 2);
            var trade = new TradeBar(nextTime, symbol, 3, 3, 3, 3, 3);

            var cryptoSecurity = new Security(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                                              new SubscriptionDataConfig(typeof(QuoteBar), symbol, Resolution.Second, TimeZones.Utc, TimeZones.Utc, true, true, false),
                                              new Cash(CashBook.AccountCurrency, 0, 1m),
                                              SymbolProperties.GetDefault(CashBook.AccountCurrency));
            cryptoSecurity.Cache.AddData(quote);
            cryptoSecurity.Cache.AddData(trade);

            var price = _fillModel.GetPrices(cryptoSecurity, OrderDirection.Sell);

            Assert.AreEqual(3, price.Open);
            Assert.AreEqual(3, price.High);
            Assert.AreEqual(3, price.Low);
            Assert.AreEqual(3, price.Close);
            Assert.AreEqual(3, price.Current);

        }

        internal class TestableLatestFillModel : LatestPriceFillModel
        {
            public Prices GetPrices(Security asset, OrderDirection direction)
            {
                return base.GetPrices(asset, direction);
            }
        }
    }
}
