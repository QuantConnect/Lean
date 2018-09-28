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
        private Symbol _symbol;
        private QuoteBar _quote;
        private TradeBar _trade;
        private TestableLatestFillModel _fillModel;

        [TestFixtureSetUp]
        public void Setup()
        {
            var time = new DateTime(2017, 1, 3, 0, 0, 0);
            var nextTime = time.AddSeconds(1);
            _symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, "GDAX");

            _quote = new QuoteBar(time, _symbol, new Bar(1, 1, 1, 1), 1, new Bar(2, 2, 2, 2), 2);
            _trade = new TradeBar(nextTime, _symbol, 3, 3, 3, 3, 3);

            _fillModel = new TestableLatestFillModel();
        }

        [Test]
        public void LatestPriceFillModel_UsesLatestPrice()
        {
            var cryptoSecurity = CreateCrypto();
            cryptoSecurity.Cache.AddData(_quote);
            cryptoSecurity.Cache.AddData(_trade);

            var price = _fillModel.GetPrices(cryptoSecurity, OrderDirection.Sell);

            // Latest price comes from the TradeBar and its prices are $3
            Assert.AreEqual(3, price.Open);
            Assert.AreEqual(3, price.High);
            Assert.AreEqual(3, price.Low);
            Assert.AreEqual(3, price.Close);
            Assert.AreEqual(3, price.Current);
        }

        [Test]
        public void LatestPriceFillModel_NullTradeBar()
        {
            var cryptoSecurity = CreateCrypto();
            cryptoSecurity.Cache.AddData(_quote);

            var price = _fillModel.GetPrices(cryptoSecurity, OrderDirection.Sell);

            // Bid prices are $1
            Assert.AreEqual(1, price.Open);
            Assert.AreEqual(1, price.High);
            Assert.AreEqual(1, price.Low);
            Assert.AreEqual(1, price.Close);
            Assert.AreEqual(1, price.Current);
        }

        [Test]
        public void LatestPriceFillModel_NullQuoteBar()
        {
            var cryptoSecurity = CreateCrypto();
            cryptoSecurity.Cache.AddData(_trade);

            var price = _fillModel.GetPrices(cryptoSecurity, OrderDirection.Sell);

            // TradeBar prices are $3
            Assert.AreEqual(3, price.Open);
            Assert.AreEqual(3, price.High);
            Assert.AreEqual(3, price.Low);
            Assert.AreEqual(3, price.Close);
            Assert.AreEqual(3, price.Current);
        }

        [Test]
        public void LatestPriceFillModel_NoData()
        {
            var cryptoSecurity = CreateCrypto();

            var price = _fillModel.GetPrices(cryptoSecurity, OrderDirection.Sell);

            // Prices are not set: $0
            Assert.AreEqual(0, price.Open);
            Assert.AreEqual(0, price.High);
            Assert.AreEqual(0, price.Low);
            Assert.AreEqual(0, price.Close);
            Assert.AreEqual(0, price.Current);
        }

        private Security CreateCrypto()
        {
            return new Security(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new SubscriptionDataConfig(typeof(QuoteBar), _symbol, Resolution.Second, TimeZones.Utc, TimeZones.Utc, true, true, false),
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
        }

        internal class TestableLatestFillModel : LatestPriceFillModel
        {
            public new Prices GetPrices(Security asset, OrderDirection direction)
            {
                return base.GetPrices(asset, direction);
            }
        }
    }
}
