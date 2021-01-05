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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class DataManagerTests
    {
        private QCAlgorithm _algorithm;
        private SecurityService _securityService;

        [SetUp]
        public void SetUp()
        {
            _algorithm = new AlgorithmStub();
            _securityService = new SecurityService(_algorithm.Portfolio.CashBook,
                MarketHoursDatabase.AlwaysOpen,
                SymbolPropertiesDatabase.FromDataFolder(),
                _algorithm,
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCacheProvider(_algorithm.Portfolio));
        }

        [Test]
        public void ReturnsExistingConfig()
        {
            var dataPermissionManager = new DataPermissionManager();
            var dataManager = new DataManager(new NullDataFeed(),
                new UniverseSelection(_algorithm,
                    _securityService,
                    dataPermissionManager,
                    new DefaultDataProvider()),
                _algorithm,
                _algorithm.TimeKeeper,
                MarketHoursDatabase.AlwaysOpen,
                false,
                new RegisteredSecurityDataTypesProvider(),
                dataPermissionManager);

            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false);

            var sameConfig = dataManager.SubscriptionManagerGetOrAdd(config);
            Assert.IsTrue(ReferenceEquals(sameConfig, config));
            Assert.AreEqual(1, dataManager.GetSubscriptionDataConfigs(config.Symbol).Count);

            var otherInstance = new SubscriptionDataConfig(config.Type,
                config.Symbol,
                config.Resolution,
                config.DataTimeZone,
                config.ExchangeTimeZone,
                config.FillDataForward,
                config.ExtendedMarketHours,
                config.IsInternalFeed);

            sameConfig = dataManager.SubscriptionManagerGetOrAdd(otherInstance);
            Assert.IsTrue(ReferenceEquals(sameConfig, config));
            Assert.AreEqual(1, dataManager.GetSubscriptionDataConfigs(config.Symbol).Count);

            dataManager.RemoveAllSubscriptions();
        }

        [Test]
        public void RemovesExistingConfig()
        {
            var dataPermissionManager = new DataPermissionManager();
            var dataFeed = new TestDataFeed();
            var dataManager = new DataManager(dataFeed,
                new UniverseSelection(_algorithm,
                    _securityService,
                    dataPermissionManager,
                    new DefaultDataProvider()),
                _algorithm,
                _algorithm.TimeKeeper,
                MarketHoursDatabase.AlwaysOpen,
                false,
                new RegisteredSecurityDataTypesProvider(),
                dataPermissionManager);

            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false);

            Assert.IsTrue(ReferenceEquals(dataManager.SubscriptionManagerGetOrAdd(config), config));
            Assert.AreEqual(1, dataManager.GetSubscriptionDataConfigs(config.Symbol).Count);

            // we didn't add any subscription yet
            Assert.IsFalse(dataManager.RemoveSubscription(config));

            var request = new SubscriptionRequest(false,
                null,
                new Security(Symbols.SPY,
                    SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                    new Cash(Currencies.USD, 1, 1),
                    SymbolProperties.GetDefault(Currencies.USD),
                    new IdentityCurrencyConverter(Currencies.USD),
                    new RegisteredSecurityDataTypesProvider(),
                    new SecurityCache()),
                config,
                new DateTime(2019, 1, 1),
                new DateTime(2019, 1, 1));

            dataFeed.Subscription = new Subscription(request,
                new EnqueueableEnumerator<SubscriptionData>(), 
                null);

            Assert.IsTrue(dataManager.AddSubscription(request));
            Assert.IsTrue(dataManager.RemoveSubscription(config));
            Assert.AreEqual(0, dataManager.GetSubscriptionDataConfigs(config.Symbol).Count);

            dataManager.RemoveAllSubscriptions();
        }

        [Test]
        // reproduces GH issue 3877
        public void ConfigurationForAddedSubscriptionIsAlwaysPresent()
        {
            var dataPermissionManager = new DataPermissionManager();
            var dataFeed = new TestDataFeed();
            var dataManager = new DataManager(dataFeed,
                new UniverseSelection(_algorithm,
                    _securityService,
                    dataPermissionManager,
                    new DefaultDataProvider()),
                _algorithm,
                _algorithm.TimeKeeper,
                MarketHoursDatabase.AlwaysOpen,
                false,
                new RegisteredSecurityDataTypesProvider(),
                dataPermissionManager);

            var config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false);

            // Universe A: adds the config
            dataManager.SubscriptionManagerGetOrAdd(config);

            var request = new SubscriptionRequest(false,
                null,
                new Security(Symbols.SPY,
                    SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                    new Cash(Currencies.USD, 1, 1),
                    SymbolProperties.GetDefault(Currencies.USD),
                    new IdentityCurrencyConverter(Currencies.USD),
                    new RegisteredSecurityDataTypesProvider(),
                    new SecurityCache()),
                config,
                new DateTime(2019, 1, 1),
                new DateTime(2019, 1, 1));

            dataFeed.Subscription = new Subscription(request,
                new EnqueueableEnumerator<SubscriptionData>(),
                null);

            // Universe A: adds the subscription
            dataManager.AddSubscription(request);

            // Universe B: adds the config
            dataManager.SubscriptionManagerGetOrAdd(config);

            // Universe A: removes the subscription
            Assert.IsTrue(dataManager.RemoveSubscription(config));
            Assert.AreEqual(0, dataManager.GetSubscriptionDataConfigs(config.Symbol).Count);

            // Universe B: adds the subscription
            Assert.IsTrue(dataManager.AddSubscription(request));

            // the config should be present
            Assert.AreEqual(1, dataManager.GetSubscriptionDataConfigs(config.Symbol).Count);

            dataManager.RemoveAllSubscriptions();
        }

        private class TestDataFeed : IDataFeed
        {
            public Subscription Subscription { get; set; }

            public bool IsActive { get; }

            public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler,
                IMapFileProvider mapFileProvider, IFactorFileProvider factorFileProvider, IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager, IDataFeedTimeProvider dataFeedTimeProvider,
                IDataChannelProvider channelProvider)
            {
            }

            public Subscription CreateSubscription(SubscriptionRequest request)
            {
                return Subscription;
            }

            public void RemoveSubscription(Subscription subscription)
            {
            }

            public void Exit()
            {
            }
        }
    }
}
