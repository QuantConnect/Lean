using System;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class BrokerageModelSecurityInitializerTests
    {
        private QCAlgorithm _algo;
        private BrokerageModelSecurityInitializer _brokerageInitializer;
        private Security _tradeBarSecurity;
        private readonly SubscriptionDataConfig _tradeBarConfig = new SubscriptionDataConfig(typeof(TradeBar),
                                                                                     Symbols.SPY,
                                                                                     Resolution.Second,
                                                                                     TimeZones.NewYork,
                                                                                     TimeZones.NewYork,
                                                                                     false,
                                                                                     false,
                                                                                     false,
                                                                                     false,
                                                                                     TickType.Trade,
                                                                                     false);

        private Security _quoteBarSecurity;
        private readonly SubscriptionDataConfig _quoteBarConfig = new SubscriptionDataConfig(typeof(QuoteBar),
                                                                                     Symbols.EURUSD,
                                                                                     Resolution.Second,
                                                                                     TimeZones.NewYork,
                                                                                     TimeZones.NewYork,
                                                                                     false,
                                                                                     false,
                                                                                     false,
                                                                                     false,
                                                                                     TickType.Quote,
                                                                                     false);

        [SetUp]
        public void Setup()
        {
            _algo =  new QCAlgorithm();
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(null,
                                       new DefaultDataProvider(),
                                       new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                                       new LocalDiskMapFileProvider(),
                                       new LocalDiskFactorFileProvider(),
                                       null);

            _algo.HistoryProvider = historyProvider;

            _tradeBarSecurity = new Security(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                                        _tradeBarConfig,
                                        new Cash(CashBook.AccountCurrency, 0, 1m),
                                        SymbolProperties.GetDefault(CashBook.AccountCurrency));

            _quoteBarSecurity = new Security(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                                    _quoteBarConfig,
                                    new Cash(CashBook.AccountCurrency, 0, 1m),
                                    SymbolProperties.GetDefault(CashBook.AccountCurrency));

            _brokerageInitializer = new BrokerageModelSecurityInitializer(new DefaultBrokerageModel(),
                                                                             new FuncSecuritySeeder(_algo.GetLastKnownPrice));
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CanSetLeverageForBacktesting_Successfully()
        {
            Assert.AreEqual(_tradeBarSecurity.Leverage, 1.0);

            _brokerageInitializer.Initialize(_tradeBarSecurity);

            Assert.AreEqual(_tradeBarSecurity.Leverage, 2.0);
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CanSetPrice_ForTradeBar()
        {
            // Arrange
            var dateForWhichDataExist = new DateTime(2013, 10, 10, 12, 0, 0);
            _algo.SetDateTime(dateForWhichDataExist);
            
            // Act
            _brokerageInitializer.Initialize(_tradeBarSecurity);

            // Assert
            Assert.IsFalse(_tradeBarSecurity.Price == 0);
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CanSetPrice_ForQuoteBar()
        {
            // Arrange
            var dateForWhichDataExist = new DateTime(2014, 5, 6, 12, 0, 0);
            _algo.SetDateTime(dateForWhichDataExist);

            // Act
            _brokerageInitializer.Initialize(_quoteBarSecurity);

            // Assert
            Assert.IsFalse(_quoteBarSecurity.Price == 0);
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CannotSetPrice_ForNonExistentHistory()
        {
            // Arrange
            var dateForWhichDataDoesNotExist = new DateTime(2050, 10, 10, 12, 0, 0);
            _algo.SetDateTime(dateForWhichDataDoesNotExist);

            // Act
            _brokerageInitializer.Initialize(_tradeBarSecurity);

            // Assert
            Assert.IsTrue(_tradeBarSecurity.Price == 0);
        }
    }
}
