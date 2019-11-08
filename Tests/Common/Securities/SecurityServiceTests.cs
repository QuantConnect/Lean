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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    public class SecurityServiceTests : ISecurityInitializerProvider
    {
        private ISecurityService _securityService;
        private SubscriptionManager _subscriptionManager;
        private MarketHoursDatabase _marketHoursDatabase;
        public ISecurityInitializer SecurityInitializer => QuantConnect.Securities.SecurityInitializer.Null;

        [SetUp]
        public void Setup()
        {
            SymbolCache.Clear();
            _subscriptionManager = new SubscriptionManager();
            var dataManager = new DataManagerStub();
            _subscriptionManager.SetDataManager(dataManager);
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _securityService = dataManager.SecurityService;
        }

        [TestCase("EURUSD", SecurityType.Forex, Market.FXCM)]
        [TestCase("EURUSD", SecurityType.Forex, Market.Oanda)]
        [TestCase("BTCUSD", SecurityType.Crypto, Market.GDAX)]
        public void CanCreate_ForexOrCrypto_WithCorrectSubscriptions(string ticker, SecurityType type, string market)
        {
            var symbol = Symbol.Create(ticker, type, market);

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(typeof(QuoteBar), symbol, Resolution.Second, false, false, false);
            var actual = _securityService.CreateSecurity(symbol, configs, 1.0m, false);

            Assert.AreEqual(actual.Subscriptions.Count(), 1);
            Assert.AreEqual(actual.Subscriptions.First().Type, typeof(QuoteBar));
            Assert.AreEqual(actual.Subscriptions.First().TickType, TickType.Quote);
        }

        [Test]
        public void CanCreate_CanonicalOption_WithCorrectSubscriptions()
        {
            var optionSymbol = Symbol.Create("GOOG", SecurityType.Option, Market.USA);

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(typeof(ZipEntryName), optionSymbol, Resolution.Minute, false, false, false);
            var option = _securityService.CreateSecurity(optionSymbol, configs, 1.0m, false);

            Assert.AreEqual(option.Subscriptions.Count(), 1);
            Assert.AreEqual(option.Subscriptions.First().Type, typeof(ZipEntryName));
            Assert.AreEqual(option.Subscriptions.First().TickType, TickType.Quote);
        }


        [Test]
        public void CanCreate_Equity_WithCorrectSubscriptions()
        {
            var equitySymbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(typeof(TradeBar), equitySymbol, Resolution.Second, false, false, false);
            var equity = _securityService.CreateSecurity(equitySymbol, configs, 1.0m, false);

            Assert.AreEqual(equity.Subscriptions.Count(), 1);
            Assert.AreEqual(equity.Subscriptions.First().Type, typeof(TradeBar));
            Assert.AreEqual(equity.Subscriptions.First().TickType, TickType.Trade);
        }

        [Test]
        public void CanCreate_Cfd_WithCorrectSubscriptions()
        {
            var symbol = Symbol.Create("abc", SecurityType.Cfd, Market.USA);
            _marketHoursDatabase.SetEntryAlwaysOpen(Market.USA, "abc", SecurityType.Cfd, TimeZones.NewYork);

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(typeof(QuoteBar), symbol, Resolution.Second, false, false, false);
            var cfd = _securityService.CreateSecurity(symbol, configs, 1.0m, false);

            Assert.AreEqual(cfd.Subscriptions.Count(), 1);
            Assert.AreEqual(cfd.Subscriptions.First().Type, typeof(QuoteBar));
            Assert.AreEqual(cfd.Subscriptions.First().TickType, TickType.Quote);
        }

        [Test]
        public void CanCreate_CustomSecurities_WithCorrectSubscriptions()
        {
            var symbol = new Symbol(SecurityIdentifier.GenerateBase(null, "BTC", Market.USA), "BTC");
            _marketHoursDatabase.SetEntryAlwaysOpen(Market.USA, "BTC", SecurityType.Base, TimeZones.NewYork);

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(typeof(LiveTradingFeaturesAlgorithm.Bitcoin), symbol, Resolution.Second, false, false, false);
            var security = _securityService.CreateSecurity(symbol, configs, 1.0m, false);

            Assert.AreEqual(security.Subscriptions.Count(), 1);
            Assert.AreEqual(security.Subscriptions.First().Type, typeof(LiveTradingFeaturesAlgorithm.Bitcoin));
            Assert.AreEqual(security.Subscriptions.First().TickType, TickType.Trade);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "Symbol can't be found in the Symbol Properties Database")]
        public void ThrowOnCreateCryptoNotDescribedInCSV()
        {
            var symbol = Symbol.Create("ABCDEFG", SecurityType.Crypto, Market.GDAX);

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(typeof(QuoteBar), symbol, Resolution.Minute, false, false, false);
            var actual = _securityService.CreateSecurity(symbol, configs, 1.0m, false);
        }

        [Test]
        public void CanCreate_ConcreteOptions_WithCorrectSubscriptions()
        {
            var underlying = SecurityIdentifier.GenerateEquity(new DateTime(1998, 01, 02), "SPY", Market.USA);
            var optionIdentifier = SecurityIdentifier.GenerateOption(new DateTime(2015, 09, 18), underlying, Market.USA, 195.50m, OptionRight.Put, OptionStyle.European);
            var optionSymbol = new Symbol(optionIdentifier, "SPY", Symbol.Empty);

            var subscriptionTypes = new List<Tuple<Type, TickType>>
            {
                new Tuple<Type, TickType>(typeof(TradeBar), TickType.Trade),
                new Tuple<Type, TickType>(typeof(QuoteBar), TickType.Quote),
                new Tuple<Type, TickType>(typeof(OpenInterest), TickType.OpenInterest)
            };

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(optionSymbol, Resolution.Minute, false, false, false, false, false, subscriptionTypes);
            var security = _securityService.CreateSecurity(optionSymbol, configs, 1.0m, false);

            Assert.IsFalse(optionSymbol.IsCanonical());

            Assert.AreEqual(security.Subscriptions.Count(), 3);
            Assert.IsTrue(security.Subscriptions.Any(x => x.TickType == TickType.OpenInterest && x.Type == typeof(OpenInterest)));
            Assert.IsTrue(security.Subscriptions.Any(x => x.TickType == TickType.Quote && x.Type == typeof(QuoteBar)));
            Assert.IsTrue(security.Subscriptions.Any(x => x.TickType == TickType.Trade && x.Type == typeof(TradeBar)));
        }

        [Test]
        public void CanCreate_ConcreteFutures_WithCorrectSubscriptions()
        {
            var identifier = SecurityIdentifier.GenerateFuture(new DateTime(2020, 12, 15), "ED", Market.USA);
            var symbol = new Symbol(identifier, "ED", Symbol.Empty);
            _marketHoursDatabase.SetEntryAlwaysOpen(Market.USA, "ED", SecurityType.Future, TimeZones.NewYork);

            var subscriptionTypes = new List<Tuple<Type, TickType>>
            {
                new Tuple<Type, TickType>(typeof(TradeBar), TickType.Trade),
                new Tuple<Type, TickType>(typeof(QuoteBar), TickType.Quote),
                new Tuple<Type, TickType>(typeof(OpenInterest), TickType.OpenInterest)
            };

            var configs = _subscriptionManager.SubscriptionDataConfigService.Add(symbol, Resolution.Second, false, false, false, false, false, subscriptionTypes);
            var security = _securityService.CreateSecurity(symbol, configs, 1.0m, false);

            Assert.IsFalse(symbol.IsCanonical());

            Assert.AreEqual(security.Subscriptions.Count(), 3);
            Assert.IsTrue(security.Subscriptions.Any(x => x.TickType == TickType.OpenInterest && x.Type == typeof(OpenInterest)));
            Assert.IsTrue(security.Subscriptions.Any(x => x.TickType == TickType.Quote && x.Type == typeof(QuoteBar)));
            Assert.IsTrue(security.Subscriptions.Any(x => x.TickType == TickType.Trade && x.Type == typeof(TradeBar)));
        }
    }
}
