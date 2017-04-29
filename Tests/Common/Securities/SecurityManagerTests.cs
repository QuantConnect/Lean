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
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class SecurityManagerTests
    {
        private SecurityManager _securityManager;
        private SecurityTransactionManager _securityTransactionManager;
        private SecurityPortfolioManager _securityPortfolioManager;
        private SubscriptionManager _subscriptionManager;
        private MarketHoursDatabase _marketHoursDatabase;
        private SymbolPropertiesDatabase _symbolPropertiesDatabase;
        private ISecurityInitializer _securityInitializer;

        [TestFixtureSetUp]
        public void Setup()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            _securityManager = new SecurityManager(timeKeeper);
            _securityTransactionManager = new SecurityTransactionManager(_securityManager);
            _securityPortfolioManager = new SecurityPortfolioManager(_securityManager, _securityTransactionManager);
            _subscriptionManager = new SubscriptionManager(timeKeeper);
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            _securityInitializer = SecurityInitializer.Null;
        }

        [Test]
        public void NotifiesWhenSecurityAdded()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var manager = new SecurityManager(timeKeeper);

            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), CreateTradeBarConfig(), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems.OfType<object>().Single() != security)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to security");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Add);
                    Assert.Pass();
                }
            };

            manager.Add(security.Symbol, security);
        }

        [Test]
        public void NotifiesWhenSecurityAddedViaIndexer()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var manager = new SecurityManager(timeKeeper);

            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), CreateTradeBarConfig(), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems.OfType<object>().Single() != security)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to security");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Add);
                    Assert.Pass();
                }
            };

            manager[security.Symbol] = security;
        }

        [Test]
        public void NotifiesWhenSecurityRemoved()
        {
            var timeKeeper = new TimeKeeper(new DateTime(2015, 12, 07));
            var manager = new SecurityManager(timeKeeper);

            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), CreateTradeBarConfig(), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            manager.Add(security.Symbol, security);
            manager.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems.OfType<object>().Single() != security)
                {
                    Assert.Fail("Expected args.NewItems to have exactly one element equal to security");
                }
                else
                {
                    Assert.IsTrue(args.Action == NotifyCollectionChangedAction.Remove);
                    Assert.Pass();
                }
            };

            manager.Remove(security.Symbol);
        }

        [Test]
        public void SecurityManagerCanCreate_Forex_WithCorrectSubscriptions()
        {
            var forexSymbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            var forexMarketHoursDbEntry = _marketHoursDatabase.GetEntry(forexSymbol.ID.Market, forexSymbol, SecurityType.Forex);
            var forexDefaultQuoteCurrency = forexSymbol.Value.Substring(3);

            var forexSymbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(forexSymbol.ID.Market, forexSymbol, forexSymbol.ID.SecurityType, forexDefaultQuoteCurrency);
            var subscriptionTypes = new List<Type>() { typeof(QuoteBar) };

            var forex = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                forexMarketHoursDbEntry.ExchangeHours,
                forexMarketHoursDbEntry.DataTimeZone,
                forexSymbolProperties,
                _securityInitializer,
                forexSymbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);
            Assert.AreEqual(forex.Subscriptions.Count(), 1);
            Assert.AreEqual(forex.Subscriptions.First().Type, typeof(QuoteBar));
            Assert.AreEqual(forex.Subscriptions.First().TickType, TickType.Quote);
        }

        [Test]
        public void SecurityManagerCanCreate_CanonicalOption_WithCorrectSubscriptions()
        {
            var optionSymbol = Symbol.Create("GOOG", SecurityType.Option, Market.USA);
            var optionMarketHoursDbEntry = _marketHoursDatabase.GetEntry(optionSymbol.ID.Market, optionSymbol, SecurityType.Option);
            var optionDefaultQuoteCurrency = CashBook.AccountCurrency;
            var optionSymbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(optionSymbol.ID.Market, optionSymbol, optionSymbol.ID.SecurityType, optionDefaultQuoteCurrency);
            var subscriptionTypes = new List<Type>() { typeof(ZipEntryName) };

            var option = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                optionMarketHoursDbEntry.ExchangeHours,
                optionMarketHoursDbEntry.DataTimeZone,
                optionSymbolProperties,
                _securityInitializer,
                optionSymbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);

            Assert.AreEqual(option.Subscriptions.Count(), 1);
            Assert.AreEqual(option.Subscriptions.First().Type, typeof(ZipEntryName));
            Assert.AreEqual(option.Subscriptions.First().TickType, TickType.Quote);
        }


        [Test]
        public void SecurityManagerCanCreate_Equity_WithCorrectSubscriptions()
        {
            var equitySymbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            var equityMarketHoursDbEntry = _marketHoursDatabase.GetEntry(equitySymbol.ID.Market, equitySymbol, SecurityType.Equity);
            var equityDefaultQuoteCurrency = CashBook.AccountCurrency;
            var equitySymbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(equitySymbol.ID.Market, equitySymbol, equitySymbol.ID.SecurityType, equityDefaultQuoteCurrency);
            var subscriptionTypes = new List<Type>() { typeof(TradeBar) };

            var equity = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                equityMarketHoursDbEntry.ExchangeHours,
                equityMarketHoursDbEntry.DataTimeZone,
                equitySymbolProperties,
                _securityInitializer,
                equitySymbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);

            Assert.AreEqual(equity.Subscriptions.Count(), 1);
            Assert.AreEqual(equity.Subscriptions.First().Type, typeof(TradeBar));
            Assert.AreEqual(equity.Subscriptions.First().TickType, TickType.Trade);
        }

        [Test]
        public void SecurityManagerCanCreate_Cfd_WithCorrectSubscriptions()
        {
            var symbol = Symbol.Create("abc", SecurityType.Cfd, Market.USA);
            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, SecurityType.Equity);
            var defaultQuoteCurrency = CashBook.AccountCurrency;
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.ID.SecurityType, defaultQuoteCurrency);
            var subscriptionTypes = new List<Type>() { typeof(TradeBar) };

            var subscriptions = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                marketHoursDbEntry.ExchangeHours,
                marketHoursDbEntry.DataTimeZone,
                symbolProperties,
                _securityInitializer,
                symbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);

            Assert.AreEqual(subscriptions.Subscriptions.Count(), 1);
            Assert.AreEqual(subscriptions.Subscriptions.First().Type, typeof(TradeBar));
            Assert.AreEqual(subscriptions.Subscriptions.First().TickType, TickType.Trade);
        }

        [Test]
        public void SecurityManagerCanCreate_CustomSecurities_WithCorrectSubscriptions()
        {
            var equitySymbol = new Symbol(SecurityIdentifier.GenerateBase("BTC", Market.USA), "BTC");
            var equityMarketHoursDbEntry = _marketHoursDatabase.GetEntry(Market.USA, "BTC", SecurityType.Base, TimeZones.NewYork);
            var equitySymbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(equitySymbol.ID.Market, equitySymbol, equitySymbol.ID.SecurityType, CashBook.AccountCurrency);
            var subscriptionTypes = new List<Type>() { typeof(Bitcoin) };

            var equity = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                equityMarketHoursDbEntry.ExchangeHours,
                equityMarketHoursDbEntry.DataTimeZone,
                equitySymbolProperties,
                _securityInitializer,
                equitySymbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);

            Assert.AreEqual(equity.Subscriptions.Count(), 1);
            Assert.AreEqual(equity.Subscriptions.First().Type, typeof(Bitcoin));
            Assert.AreEqual(equity.Subscriptions.First().TickType, TickType.Trade);
        }

        [Test]
        public void SecurityManagerCanCreate_ConcreteOptions_WithCorrectSubscriptions()
        {
            var underlying = SecurityIdentifier.GenerateEquity(new DateTime(1998, 01, 02), "SPY", Market.USA);
            var optionIdentifier = SecurityIdentifier.GenerateOption(new DateTime(2015, 09, 18), underlying, Market.USA, 195.50m, OptionRight.Put, OptionStyle.European);
            var optionSymbol = new Symbol(optionIdentifier, "SPY", Symbol.Empty);
            var optionMarketHoursDbEntry = _marketHoursDatabase.GetEntry(Market.USA, "SPY", SecurityType.Equity, TimeZones.NewYork);
            var optionSymbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(optionSymbol.ID.Market, "SPY", optionSymbol.ID.SecurityType, CashBook.AccountCurrency);

            var subscriptionTypes = new List<Type>() { typeof(TradeBar), typeof(QuoteBar), typeof(OpenInterest) };

            var optionSubscriptions = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                optionMarketHoursDbEntry.ExchangeHours,
                optionMarketHoursDbEntry.DataTimeZone,
                optionSymbolProperties,
                _securityInitializer,
                optionSymbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);

            Assert.IsFalse(optionSymbol.IsCanonical());

            Assert.AreEqual(optionSubscriptions.Subscriptions.Count(), 3);
            Assert.IsTrue(optionSubscriptions.Subscriptions.Any(x => x.TickType == TickType.OpenInterest && x.Type == typeof(OpenInterest)));
            Assert.IsTrue(optionSubscriptions.Subscriptions.Any(x => x.TickType == TickType.Quote && x.Type == typeof(QuoteBar)));
            Assert.IsTrue(optionSubscriptions.Subscriptions.Any(x => x.TickType == TickType.Trade && x.Type == typeof(TradeBar)));
        }

        [Test]
        public void SecurityManagerCanCreate_ConcreteFutures_WithCorrectSubscriptions()
        {
            var identifier = SecurityIdentifier.GenerateFuture(new DateTime(2020, 12, 15), "ED", Market.USA);
            var symbol = new Symbol(identifier, "ED", Symbol.Empty);
            var marketHoursDbEntry = _marketHoursDatabase.GetEntry(Market.USA, "ED", SecurityType.Equity, TimeZones.NewYork);
            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, "ED", symbol.ID.SecurityType, CashBook.AccountCurrency);

            var subscriptionTypes = new List<Type>() { typeof(TradeBar), typeof(QuoteBar), typeof(OpenInterest) };

            var subscriptions = SecurityManager.CreateSecurity(subscriptionTypes,
                _securityPortfolioManager,
                _subscriptionManager,
                marketHoursDbEntry.ExchangeHours,
                marketHoursDbEntry.DataTimeZone,
                symbolProperties,
                _securityInitializer,
                symbol,
                Resolution.Second,
                false,
                1.0m,
                false,
                false,
                false,
                false);

            Assert.IsFalse(symbol.IsCanonical());

            Assert.AreEqual(subscriptions.Subscriptions.Count(), 3);
            Assert.IsTrue(subscriptions.Subscriptions.Any(x => x.TickType == TickType.OpenInterest && x.Type == typeof(OpenInterest)));
            Assert.IsTrue(subscriptions.Subscriptions.Any(x => x.TickType == TickType.Quote && x.Type == typeof(QuoteBar)));
            Assert.IsTrue(subscriptions.Subscriptions.Any(x => x.TickType == TickType.Trade && x.Type == typeof(TradeBar)));
        }

        private SubscriptionDataConfig CreateTradeBarConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
