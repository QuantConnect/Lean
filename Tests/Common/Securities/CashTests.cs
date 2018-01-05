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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashTests
    {
        private static readonly DateTimeZone TimeZone = TimeZones.NewYork;
        private static readonly SecurityExchangeHours SecurityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZone);
        private static readonly IReadOnlyDictionary<SecurityType, string> MarketMap = DefaultBrokerageModel.DefaultMarketMap;
        private static readonly AlgorithmSettings AlgorithmSettings = new AlgorithmSettings();
        private static readonly MarketHoursDatabase AlwaysOpenMarketHoursDatabase = MarketHoursDatabase.AlwaysOpen;

        [Test]
        public void ConstructorCapitalizedSymbol()
        {
            var cash = new Cash("low", 0, 0);
            Assert.AreEqual("LOW", cash.Symbol);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "Cash symbols must be exactly 3 characters")]
        public void ConstructorThrowsOnSymbolTooLong()
        {
            var cash = new Cash("too long", 0, 0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "Cash symbols must be exactly 3 characters")]
        public void ConstructorThrowsOnSymbolTooShort()
        {
            var cash = new Cash("s", 0, 0);
        }

        [Test]
        public void ConstructorSetsProperties()
        {
            const string symbol = "JPY";
            const int quantity = 1;
            const decimal conversionRate = 1.2m;
            var cash = new Cash(symbol, quantity, conversionRate);
            Assert.AreEqual(symbol, cash.Symbol);
            Assert.AreEqual(quantity, cash.Amount);
            Assert.AreEqual(conversionRate, cash.ConversionRate);
        }

        [Test]
        public void ComputesValueInBaseCurrency()
        {
            const int quantity = 100;
            const decimal conversionRate = 1/100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            Assert.AreEqual(quantity*conversionRate, cash.ValueInAccountCurrency);
        }

        [Test]
        public void EnsureCurrencyDataFeedAddsSubscription()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);
            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var abcConfig = subscriptions.Add(Symbols.SPY, Resolution.Minute, TimeZone, TimeZone);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(Symbols.SPY, new Security(SecurityExchangeHours, abcConfig, new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            cash.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);
            Assert.AreEqual(1, subscriptions.Subscriptions.Count(x => x.Symbol == Symbols.USDJPY));
            Assert.AreEqual(1, securities.Values.Count(x => x.Symbol == Symbols.USDJPY));
        }

        [Test]
        public void EnsureCurrencyDataFeedsAddsSubscriptionAtMinimumResolution()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            const Resolution minimumResolution = Resolution.Second;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(Symbols.SPY, new Security(SecurityExchangeHours, subscriptions.Add(Symbols.SPY, Resolution.Minute, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));
            securities.Add(Symbols.EURUSD, new Security(SecurityExchangeHours, subscriptions.Add(Symbols.EURUSD, minimumResolution, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));

            cash.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);
            Assert.AreEqual(minimumResolution, subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.USDJPY).Resolution);
        }

        [Test]
        public void EnsureCurrencyDataFeedMarksIsCurrencyDataFeedForNewSubscriptions()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(Symbols.EURUSD, new Security(SecurityExchangeHours, subscriptions.Add(Symbols.EURUSD, Resolution.Minute, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));

            cash.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);
            var config = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.USDJPY);
            Assert.IsTrue(config.IsInternalFeed);
        }

        [Test]
        public void EnsureCurrencyDataFeedDoesNotMarkIsCurrencyDataFeedForExistantSubscriptions()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(Symbols.USDJPY, new Security(SecurityExchangeHours, subscriptions.Add(Symbols.USDJPY, Resolution.Minute, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));

            cash.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);
            var config = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.USDJPY);
            Assert.IsFalse(config.IsInternalFeed);
        }

        [Test]
        public void EnsureInternalCurrencyDataFeedsForNonUsdQuoteCurrencyGetAdded()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cashJPY = new Cash("JPY", quantity, conversionRate);
            var cashGBP = new Cash("GBP", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cashJPY);
            cashBook.Add("GBP", cashGBP);

            var symbol = Symbol.Create("GBPJPY", SecurityType.Forex, Market.FXCM);

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(symbol, new Security(SecurityExchangeHours, subscriptions.Add(symbol, Resolution.Minute, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));

            cashJPY.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);
            var config1 = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.USDJPY);
            Assert.IsTrue(config1.IsInternalFeed);

            cashGBP.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);
            var config2 = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.GBPUSD);
            Assert.IsTrue(config2.IsInternalFeed);
        }

        [Test]
        public void EnsureCurrencyDataFeedForCryptoCurrency()
        {
            var book = new CashBook
            {
                {"USD", new Cash("USD", 100, 1) },
                {"BTC", new Cash("BTC", 100, 6000) },
                {"LTC", new Cash("LTC", 100, 55) },
                {"ETH", new Cash("ETH", 100, 290) },
                {"EUR", new Cash("EUR", 100, 1.2m) },
                {"JPY", new Cash("JPY", 100, 0.0088m) },
                {"XAG", new Cash("XAG", 100, 1275) },
                {"XAU", new Cash("XAU", 100, 17) }
            };

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);

            book.EnsureCurrencyDataFeeds(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap);

            var symbols = subscriptions.Subscriptions.Select(sdc => sdc.Symbol).ToHashSet();

            Assert.IsTrue(symbols.Contains(Symbols.BTCUSD));
            Assert.IsTrue(symbols.Contains(Symbols.LTCUSD));
            Assert.IsTrue(symbols.Contains(Symbols.ETHUSD));
            Assert.IsTrue(symbols.Contains(Symbols.EURUSD));
            Assert.IsTrue(symbols.Contains(Symbols.XAGUSD));
            Assert.IsTrue(symbols.Contains(Symbols.XAUUSD));
        }

        [Test]
        public void UpdateModifiesConversionRateAsInvertedValue()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(Symbols.USDJPY, new Security(SecurityExchangeHours, subscriptions.Add(Symbols.USDJPY, Resolution.Minute, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));

            // we need to get subscription index
            cash.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);

            var last = 120m;
            cash.Update(new Tick(DateTime.Now, Symbols.USDJPY, last, 119.95m, 120.05m));

            // jpy is inverted, so compare on the inverse
            Assert.AreEqual(1 / last, cash.ConversionRate);
        }

        [Test]
        public void UpdateModifiesConversionRate()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("GBP", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("GBP", cash);

            var subscriptions = new SubscriptionManager(AlgorithmSettings, TimeKeeper);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(Symbols.GBPUSD, new Security(SecurityExchangeHours, subscriptions.Add(Symbols.GBPUSD, Resolution.Minute, TimeZone, TimeZone), new Cash(CashBook.AccountCurrency, 0, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency)));

            // we need to get subscription index
            cash.EnsureCurrencyDataFeed(securities, subscriptions, AlwaysOpenMarketHoursDatabase, SymbolPropertiesDatabase.FromDataFolder(), MarketMap, cashBook);

            var last = 1.5m;
            cash.Update(new Tick(DateTime.Now, Symbols.GBPUSD, last, last * 1.009m, last * 0.009m));

            // jpy is inverted, so compare on the inverse
            Assert.AreEqual(last, cash.ConversionRate);
        }

        [TestCase("USD", "$")]
        [TestCase("EUR", "€")]
        [TestCase("GBP", "₤")]
        [TestCase("BTC", "฿")]
        public void CashHasCorrectCurrencySymbol(string symbol, string currencySymbol)
        {
            var cash = new Cash(symbol, 1, 1);
            Assert.AreEqual(currencySymbol, cash.CurrencySymbol);
        }

        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZone }); }
        }
    }
}
