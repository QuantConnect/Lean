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
        private Security _security;
        private readonly SubscriptionDataConfig _config = new SubscriptionDataConfig(typeof(TradeBar),
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

        [SetUp]
        public void Setup()
        {
            _algo =  new QCAlgorithm();
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(null,
                                       new LocalDiskMapFileProvider(),
                                       new LocalDiskFactorFileProvider(),
                                       new DefaultDataFileProvider(),
                                       null,
                                       new DefaultDataCacheProvider());

            _algo.HistoryProvider = historyProvider;

            _security = new Security(SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                                        _config,
                                        new Cash(CashBook.AccountCurrency, 0, 1m),
                                        SymbolProperties.GetDefault(CashBook.AccountCurrency));

            _brokerageInitializer = new BrokerageModelSecurityInitializer(new DefaultBrokerageModel(),
                                                                             new FuncSecuritySeeder(_algo.GetLastKnownPrice));
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CanSetLeverageForBacktesting_Successfully()
        {
            Assert.AreEqual(_security.Leverage, 1.0);

            _brokerageInitializer.Initialize(_security);

            Assert.AreEqual(_security.Leverage, 2.0);
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CanSetPrice_ForExistingHistory()
        {
            // Arrange
            var dateForWhichDataExist = DateTime.Parse("10/10/2013 12:00PM");
            _algo.SetDateTime(dateForWhichDataExist);
            
            // Act
            _brokerageInitializer.Initialize(_security);

            // Assert
            Assert.IsFalse(_security.Price == 0);
        }

        [Test]
        public void BrokerageModelSecurityInitializer_CannotSetPrice_ForNonExistentHistory()
        {
            // Arrange
            var dateForWhichDataDoesNotExist = DateTime.Parse("10/10/2050 12:00PM");
            _algo.SetDateTime(dateForWhichDataDoesNotExist);

            // Act
            _brokerageInitializer.Initialize(_security);

            // Assert
            Assert.IsTrue(_security.Price == 0);
        }
    }
}
