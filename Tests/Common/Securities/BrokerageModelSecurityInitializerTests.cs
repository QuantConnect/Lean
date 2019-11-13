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
 *
*/

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
using QuantConnect.Tests.Engine.DataFeeds;

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
                                                                                     DateTimeZone.ForOffset(Offset.FromHours(-5)),
                                                                                     DateTimeZone.ForOffset(Offset.FromHours(-5)),
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
            historyProvider.Initialize(
                new HistoryProviderInitializeParameters(
                    null,
                    null,
                    new DefaultDataProvider(),
                    new SingleEntryDataCacheProvider(new DefaultDataProvider()),
                    new LocalDiskMapFileProvider(),
                    new LocalDiskFactorFileProvider(),
                    null,
                    true
                )
            );

            _algo.HistoryProvider = historyProvider;
            _algo.SubscriptionManager.SetDataManager(new DataManagerStub(_algo));
            _tradeBarSecurity = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                _tradeBarConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            _quoteBarSecurity = new Security(
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.ForOffset(Offset.FromHours(-5))),
                _quoteBarConfig,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

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

        [Test]
        public void BrokerageModelSecurityInitializer_SetLeverageForBuyingPowerModel_Successfully()
        {
            var brokerageModel = new DefaultBrokerageModel(AccountType.Cash);
            var localBrokerageInitializer = new BrokerageModelSecurityInitializer(brokerageModel,
                new FuncSecuritySeeder(_algo.GetLastKnownPrice));
            Assert.AreEqual(1.0, _tradeBarSecurity.Leverage);
            localBrokerageInitializer.Initialize(_tradeBarSecurity);
            Assert.AreEqual(1.0, _tradeBarSecurity.Leverage);
            Assert.AreEqual(1.0, _tradeBarSecurity.BuyingPowerModel.GetLeverage(_tradeBarSecurity));
        }

    }
}
