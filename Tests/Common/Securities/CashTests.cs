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
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using QuantConnect.Securities.CurrencyConversion;
using QuantConnect.Tests.Common.Data.UniverseSelection;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class CashTests
    {
        private static readonly DateTimeZone TimeZone = TimeZones.NewYork;
        private static readonly SecurityExchangeHours SecurityExchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZone);
        private static readonly IReadOnlyDictionary<SecurityType, string> MarketMap = DefaultBrokerageModel.DefaultMarketMap;

        [Test]
        public void ConstructorCapitalizedSymbol()
        {
            var cash = new Cash("low", 0, 0);
            Assert.AreEqual("LOW", cash.Symbol);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ConstructorThrowsOnEmptySymbol(string currency)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var cash = new Cash(currency, 0, 0);
            }, "Cash symbols cannot be null or empty.");
        }

        [Test]
        [TestCase("too long")]
        [TestCase("s")]
        public void ConstructorOnCustomSymbolLength(string currency)
        {
            var cash = new Cash(currency, 0, 0);
            Assert.AreEqual(currency.ToUpper(CultureInfo.InvariantCulture), cash.Symbol);
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
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            Assert.AreEqual(quantity * conversionRate, cash.ValueInAccountCurrency);
        }

        [Test]
        public void EnsureCurrencyDataFeedAddsSubscription()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);
            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var abcConfig = subscriptions.Add(Symbols.SPY, Resolution.Minute, TimeZone, TimeZone);
            var securities = new SecurityManager(TimeKeeper);

            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    abcConfig,
                    new Cash(Currencies.USD, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    cashBook,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()));
            cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);

            Assert.AreEqual(1, subscriptions.SubscriptionDataConfigService.GetSubscriptionDataConfigs(Symbols.USDJPY, includeInternalConfigs:true).Count);
            Assert.AreEqual(1, securities.Values.Count(x => x.Symbol == Symbols.USDJPY));
        }

        [Test]
        public void EnsureCurrencyDataFeedChecksSecurityChangesForSecurity()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);
            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var abcConfig = subscriptions.Add(Symbols.SPY, Resolution.Minute, TimeZone, TimeZone);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    abcConfig,
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            var usdjpy = new Security(Symbols.USDJPY, SecurityExchangeHours, new Cash("JPY", 0, 0), SymbolProperties.GetDefault("JPY"), ErrorCurrencyConverter.Instance, RegisteredSecurityDataTypesProvider.Null, new SecurityCache());
            var changes = SecurityChangesTests.CreateNonInternal(new[] { usdjpy }, Enumerable.Empty<Security>());
            var addedSecurities = cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, changes, dataManager.SecurityService, cashBook.AccountCurrency);

            // the security exists in SecurityChanges so it is NOT added to the security manager or subscriptions
            // this security will be added by the algorithm manager
            Assert.True(addedSecurities == null || addedSecurities.Count == 0);
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

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(
                Symbols.SPY,
                new Security(
                    SecurityExchangeHours,
                    subscriptions.Add(Symbols.SPY, Resolution.Minute, TimeZone, TimeZone),
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );
            securities.Add(
                Symbols.EURUSD,
                new Security(
                    SecurityExchangeHours,
                    subscriptions.Add(Symbols.EURUSD, minimumResolution, TimeZone, TimeZone),
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );

            cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
            Assert.AreEqual(minimumResolution, subscriptions.SubscriptionDataConfigService.GetSubscriptionDataConfigs(Symbols.USDJPY, includeInternalConfigs: true).Single().Resolution);
        }

        [Test]
        public void EnsureCurrencyDataFeedMarksIsCurrencyDataFeedForNewSubscriptions()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(
                Symbols.EURUSD,
                new Security(
                    SecurityExchangeHours,
                    subscriptions.Add(Symbols.EURUSD, Resolution.Minute, TimeZone, TimeZone),
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );

            cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
            var config = subscriptions.SubscriptionDataConfigService.GetSubscriptionDataConfigs(Symbols.USDJPY, includeInternalConfigs: true).Single();
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

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(
                Symbols.USDJPY,
                new Security(
                    SecurityExchangeHours,
                    subscriptions.Add(Symbols.USDJPY, Resolution.Minute, TimeZone, TimeZone),
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );

            cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
            var config = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.USDJPY);
            Assert.IsFalse(config.IsInternalFeed);
        }

        [TestCase("USD", "GBP", "JPY", "GBPUSD", "USDJPY", SecurityType.Forex, Market.FXCM),
         TestCase("EUR", "GBP", "JPY", "EURGBP", "EURJPY", SecurityType.Forex, Market.FXCM),
         TestCase("AUD", "GBP", "USD", "GBPAUD", "AUDUSD", SecurityType.Forex, Market.FXCM),
         TestCase("AUD", "JPY", "EUR", "AUDJPY", "EURAUD", SecurityType.Forex, Market.FXCM),
         TestCase("CHF", "JPY", "EUR", "CHFJPY", "EURCHF", SecurityType.Forex, Market.FXCM),
         TestCase("SGD", "JPY", "EUR", "SGDJPY", "EURSGD", SecurityType.Forex, Market.Oanda),
         TestCase("BTC", "USD", "EUR", "BTCUSD", "BTCEUR", SecurityType.Crypto, Market.Bitfinex),
         TestCase("EUR", "BTC", "ETH", "BTCEUR", "ETHEUR", SecurityType.Crypto, Market.Bitfinex),
         TestCase("USD", "BTC", "ETH", "BTCUSD", "ETHUSD", SecurityType.Crypto, Market.Bitfinex),
         TestCase("ETH", "USD", "BTC", "ETHUSD", "ETHBTC", SecurityType.Crypto, Market.Bitfinex),
         TestCase("LTC", "USD", "BTC", "LTCUSD", "LTCBTC", SecurityType.Crypto, Market.Bitfinex),
         TestCase("ETH", "BTC", "EOS", "ETHBTC", "EOSETH", SecurityType.Crypto, Market.Bitfinex)]
        public void NonUsdAccountCurrencyCurrencyDataFeedsGetAdded(string accountCurrency,
            string quoteCurrency,
            string baseCurrency,
            string quoteCurrencySymbol,
            string baseCurrencySymbol,
            SecurityType securityType,
            string market)
        {
            var quoteCash = new Cash(quoteCurrency, 100, 1);
            var baseCash = new Cash(baseCurrency, 100, 1);
            var cashBook = new CashBook {{quoteCurrency, quoteCash},
                { baseCurrency, baseCash}};

            var symbol = Symbol.Create(baseCurrency + quoteCurrency,
                securityType,
                market);
            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper)
            {
                {
                    symbol, new Security(
                        SecurityExchangeHours,
                        subscriptions.Add(symbol, Resolution.Minute, TimeZone, TimeZone),
                        new Cash(cashBook.AccountCurrency, 0, 1m),
                        SymbolProperties.GetDefault(cashBook.AccountCurrency),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCache()
                    )
                }
            };

            var configs1 = quoteCash.EnsureCurrencyDataFeed(securities,
                subscriptions,
                MarketMap,
                SecurityChanges.None,
                dataManager.SecurityService,
                accountCurrency);
            Assert.AreEqual(1, configs1.Count);

            var config1 = configs1[0];
            Assert.IsNotNull(config1);
            Assert.AreEqual(quoteCurrencySymbol, config1.Symbol.Value);

            var configs2 = baseCash.EnsureCurrencyDataFeed(securities,
                subscriptions,
                MarketMap,
                SecurityChanges.None,
                dataManager.SecurityService,
                accountCurrency);
            Assert.AreEqual(1, configs2.Count);

            var config2 = configs2[0];
            Assert.IsNotNull(config2);
            Assert.AreEqual(baseCurrencySymbol, config2.Symbol.Value);
        }

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

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(
                symbol,
                new Security(
                    SecurityExchangeHours,
                    subscriptions.Add(symbol, Resolution.Minute, TimeZone, TimeZone),
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );

            cashJPY.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
            var config1 = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.USDJPY);
            Assert.IsTrue(config1.IsInternalFeed);

            cashGBP.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
            var config2 = subscriptions.Subscriptions.Single(x => x.Symbol == Symbols.GBPUSD);
            Assert.IsTrue(config2.IsInternalFeed);
        }

        [Test]
        public void EnsureCurrencyDataFeedsForNonUsdQuoteCurrencyDoNotGetAddedToSymbolCache()
        {
            SymbolCache.Clear();
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cashJPY = new Cash("JPY", quantity, conversionRate);
            var cashGBP = new Cash("GBP", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cashJPY);
            cashBook.Add("GBP", cashGBP);

            var symbol = Symbol.Create("GBPJPY", SecurityType.Forex, Market.FXCM);

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            securities.Add(
                symbol,
                new Security(
                    SecurityExchangeHours,
                    subscriptions.Add(symbol, Resolution.Minute, TimeZone, TimeZone),
                    new Cash(cashBook.AccountCurrency, 0, 1m),
                    SymbolProperties.GetDefault(cashBook.AccountCurrency),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                )
            );


            Assert.IsNotNull(
                cashGBP.EnsureCurrencyDataFeed(
                    securities,
                    subscriptions,
                    MarketMap,
                    SecurityChanges.None,
                    dataManager.SecurityService,
                    cashBook.AccountCurrency));
            Assert.IsNotNull(
                cashJPY.EnsureCurrencyDataFeed(securities,
                    subscriptions,
                    MarketMap,
                    SecurityChanges.None,
                    dataManager.SecurityService,
                    cashBook.AccountCurrency));
            Assert.IsFalse(SymbolCache.TryGetSymbol("USDJPY", out symbol));
            Assert.IsFalse(SymbolCache.TryGetSymbol("GBPUSD", out symbol));
        }

        [Test]
        public void EnsureCurrencyDataFeedForCryptoCurrency()
        {
            var book = new CashBook
            {
                {Currencies.USD, new Cash(Currencies.USD, 100, 1) },
                {"BTC", new Cash("BTC", 100, 6000) },
                {"LTC", new Cash("LTC", 100, 55) },
                {"ETH", new Cash("ETH", 100, 290) },
                {"EUR", new Cash("EUR", 100, 1.2m) },
                {"JPY", new Cash("JPY", 100, 0.0088m) },
                {"XAG", new Cash("XAG", 100, 1275) },
                {"XAU", new Cash("XAU", 100, 17) }
            };

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);

            book.EnsureCurrencyDataFeeds(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService);

            var symbols = dataManager.SubscriptionManagerSubscriptions.Select(sdc => sdc.Symbol).ToHashSet();

            Assert.IsTrue(symbols.Contains(Symbols.BTCUSD));
            Assert.IsTrue(symbols.Contains(Symbols.LTCUSD));
            Assert.IsTrue(symbols.Contains(Symbols.ETHUSD));
            Assert.IsTrue(symbols.Contains(Symbols.EURUSD));
            Assert.IsTrue(symbols.Contains(Symbols.XAGUSD));
            Assert.IsTrue(symbols.Contains(Symbols.XAUUSD));

            foreach (var subscription in subscriptions.Subscriptions)
            {
                Assert.AreEqual(
                    subscription.Symbol.SecurityType == SecurityType.Crypto ? TickType.Trade : TickType.Quote,
                    subscription.TickType);
            }
        }

        [Test]
        public void UpdateModifiesConversionRateAsInvertedValue()
        {
            const int quantity = 100;
            const decimal conversionRate = 1 / 100m;
            var cash = new Cash("JPY", quantity, conversionRate);
            var cashBook = new CashBook();
            cashBook.Add("JPY", cash);

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            var security = new Security(
                SecurityExchangeHours,
                subscriptions.Add(Symbols.USDJPY, Resolution.Minute, TimeZone, TimeZone),
                new Cash(cashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(cashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            securities.Add(Symbols.USDJPY, security);

            // we need to get subscription index
            cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);

            var last = 120m;
            security.SetMarketPrice(new Tick(DateTime.Now, Symbols.USDJPY, last, 119.95m, 120.05m));
            cash.Update();

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

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);
            var security = new Security(
                SecurityExchangeHours,
                subscriptions.Add(Symbols.GBPUSD, Resolution.Minute, TimeZone, TimeZone),
                new Cash(cashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(cashBook.AccountCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            securities.Add(Symbols.GBPUSD, security);

            // we need to get subscription index
            cash.EnsureCurrencyDataFeed(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);

            var last = 1.5m;
            security.SetMarketPrice(new Tick(DateTime.Now, Symbols.GBPUSD, last, last * 1.009m, last * 0.009m));
            cash.Update();

            // jpy is inverted, so compare on the inverse
            Assert.AreEqual(last, cash.ConversionRate);
        }

        [TestCase("USD", "$")]
        [TestCase("EUR", "€")]
        [TestCase("GBP", "₤")]
        [TestCase("BTC", "₿")]
        public void CashHasCorrectCurrencySymbol(string symbol, string currencySymbol)
        {
            var cash = new Cash(symbol, 1, 1);
            Assert.AreEqual(currencySymbol, cash.CurrencySymbol);
        }

        [Test]
        public void UpdateEventCalledForSetAmountMethod()
        {
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cash.Updated += (sender, args) =>
            {
                called = true;
            };
            cash.SetAmount(10m);
            Assert.IsTrue(called);
        }

        [Test]
        public void UpdateEventCalledForAddAmountMethod()
        {
            var called = false;
            var cash = new Cash(Currencies.USD, 1, 1);
            cash.Updated += (sender, args) =>
            {
                called = true;
            };
            cash.AddAmount(10m);
            Assert.IsTrue(called);
        }

        [Test]
        public void CashBookWithUsdCanBeSerializedAfterEnsureCurrencyDataFeed()
        {
            var book = new CashBook
            {
                {Currencies.USD, new Cash(Currencies.USD, 100, 1) },
                {"EUR", new Cash("EUR", 100, 1.2m) }
            };
            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);

            book.EnsureCurrencyDataFeeds(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService);

            Assert.DoesNotThrow(() => JsonConvert.SerializeObject(book, Formatting.Indented));
        }

        [Test]
        public void EnsureCurrencyDataFeedDoesNothingWithUnsupportedCurrency()
        {
            var book = new CashBook
            {
                {Currencies.USD, new Cash(Currencies.USD, 100, 1) },
                {"ILS", new Cash("ILS", 0, 0.3m) }
            };
            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);

            var added = book.EnsureCurrencyDataFeeds(securities, subscriptions, MarketMap, SecurityChanges.None, dataManager.SecurityService);
            Assert.IsEmpty(added);
        }

        [TestCaseSource(nameof(cryptoBrokerageStableCoinCases))]
        public void CryptoStableCoinMappingIsCorrect(IBrokerageModel brokerageModel, string accountCurrency, string stableCoin, bool shouldThrow, Symbol[] expectedConversionSymbols)
        {
            var cashBook = new CashBook() {AccountCurrency = accountCurrency};
            var cash = new Cash(stableCoin, 10m, 1m);
            cashBook.Add(cash.Symbol, cash);

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub(TimeKeeper);
            subscriptions.SetDataManager(dataManager);
            var securities = new SecurityManager(TimeKeeper);

            // Verify the behavior throws or doesn't throw depending on the case
            if (shouldThrow)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    cash.EnsureCurrencyDataFeed(securities, subscriptions, brokerageModel.DefaultMarkets, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
                });
            }
            else
            {
                Assert.DoesNotThrow(() =>
                {
                    cash.EnsureCurrencyDataFeed(securities, subscriptions, brokerageModel.DefaultMarkets, SecurityChanges.None, dataManager.SecurityService, cashBook.AccountCurrency);
                });
            }

            // Verify the conversion symbol is correct
            if (expectedConversionSymbols == null)
            {
                Assert.IsInstanceOf(typeof(ConstantCurrencyConversion), cash.CurrencyConversion);
                Assert.AreEqual(accountCurrency, cash.CurrencyConversion.SourceCurrency);
                Assert.AreEqual(stableCoin, cash.CurrencyConversion.DestinationCurrency);
                Assert.AreEqual(1m, cash.ConversionRate);
                CollectionAssert.IsEmpty(cash.CurrencyConversion.ConversionRateSecurities);
            }
            else
            {
                Assert.IsInstanceOf(typeof(SecurityCurrencyConversion), cash.CurrencyConversion);

                var actualConversionSymbols = cash.CurrencyConversion
                    .ConversionRateSecurities
                    .Select(x => x.Symbol)
                    .ToArray();

                Assert.AreEqual(expectedConversionSymbols, actualConversionSymbols);
            }
        }

        private static TimeKeeper TimeKeeper
        {
            get { return new TimeKeeper(DateTime.Now, new[] { TimeZone }); }
        }

        // Crypto brokerage model stable coin and account currency cases
        // The last var is expectedConversionSymbol, and is null when we expect there
        // not to be an (indirect) conversion for our tests output
        private static object[] cryptoBrokerageStableCoinCases =
        {
            // *** Bitfinex ***
            // Trades USDC, EURS, USDT y XCHF
            // USDC Cases
            new object[] { new BitfinexBrokerageModel(), Currencies.USD, "USDC", false, new[] { Symbol.Create("USDCUSD", SecurityType.Crypto, Market.Bitfinex) } },
            new object[] { new BitfinexBrokerageModel(), Currencies.EUR, "USDC", false, new[] { Symbol.Create("USDCUSD", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda) } }, // No USDCEUR, but indirect conversion exists
            new object[] { new BitfinexBrokerageModel(), Currencies.GBP, "USDC", false, new[] { Symbol.Create("USDCUSD", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda) } }, // No USDCGBP, but indirect conversion exists

            // EURS Cases
            new object[] { new BitfinexBrokerageModel(), Currencies.USD, "EURS", false, new[] { Symbol.Create("EURSUSD", SecurityType.Crypto, Market.Bitfinex) } },
            new object[] { new BitfinexBrokerageModel(), Currencies.EUR, "EURS", false, null }, // No EURSEUR, but does not throw! Conversion 1-1
            new object[] { new BitfinexBrokerageModel(), Currencies.GBP, "EURS", false, new[] { Symbol.Create("EURSUSD", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("GBPUSD", SecurityType.Forex, Market.Oanda) } }, // No EURSGBP, but indirect conversion exists

            // USDT (Tether) Cases
            new object[] { new BitfinexBrokerageModel(), Currencies.CNH, "USDT", false, new[] { Symbol.Create("USDTCNHT", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("CNHCNHT", SecurityType.Crypto, Market.Bitfinex) } }, // No USDTCNH, but indirect conversion exists
            new object[] { new BitfinexBrokerageModel(), Currencies.USD, "USDT", false, new[] { Symbol.Create("USDTUSD", SecurityType.Crypto, Market.Bitfinex) } },
            new object[] { new BitfinexBrokerageModel(), Currencies.EUR, "USDT", false, new[] { Symbol.Create("EURUSDT", SecurityType.Crypto, Market.Bitfinex) } },
            new object[] { new BitfinexBrokerageModel(), Currencies.GBP, "USDT", false, new[] { Symbol.Create("GBPUSDT", SecurityType.Crypto, Market.Bitfinex) } },

            // XCHF Cases
            new object[] { new BitfinexBrokerageModel(), "CHF", "XCHF", false, null }, // No XCHFCHF, but does not throw! Conversion 1-1
            new object[] { new BitfinexBrokerageModel(), Currencies.EUR, "XCHF", false, new[] { Symbol.Create("BTCXCHF", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Bitfinex) } }, // No XCHFEUR, but indirect conversion exists
            new object[] { new BitfinexBrokerageModel(), Currencies.GBP, "XCHF", false, new[] { Symbol.Create("BTCXCHF", SecurityType.Crypto, Market.Bitfinex), Symbol.Create("BTCGBP", SecurityType.Crypto, Market.Bitfinex) } }, // No XCHFGBP, but indirect conversion exists

            // *** Coinbase ***
            // Trades USDC and USDT* (*Not yet trading live, but expected soon)
            // USDC Cases
            new object[] { new CoinbaseBrokerageModel(), Currencies.USD, "USDC", false, null }, // No USDCUSD, but does not throw! Conversion 1-1
            new object[] { new CoinbaseBrokerageModel(), Currencies.EUR, "USDC", false, new[] { Symbol.Create("USDCEUR", SecurityType.Crypto, Market.Coinbase) } },
            new object[] { new CoinbaseBrokerageModel(), Currencies.GBP, "USDC", false, new[] { Symbol.Create("USDCGBP", SecurityType.Crypto, Market.Coinbase) } },

            // *** Binance ***
            // USDC Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "USDC", false, null }, // No USDCUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), Currencies.EUR, "USDC", false, new[] { Symbol.Create("ADAUSDC", SecurityType.Crypto, Market.Binance), Symbol.Create("ADAEUR", SecurityType.Crypto, Market.Binance) } }, // No USDCEUR, but indirect conversion exists
            new object[] { new BinanceBrokerageModel(), Currencies.GBP, "USDC", false, new[] { Symbol.Create("ADAUSDC", SecurityType.Crypto, Market.Binance), Symbol.Create("ADAGBP", SecurityType.Crypto, Market.Binance) } }, // No USDCGBP, but indirect conversion exists

            // USDT Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "USDT", false, null }, // No USDTUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), "VIA", "USDT", false, new[] { Symbol.Create("BNBUSDT", SecurityType.Crypto, Market.Binance), Symbol.Create("VIABNB", SecurityType.Crypto, Market.Binance) } }, // No USDTVIA, but indirect conversion exists

            // USDP Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "USDP", false, null }, // No USDPUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), "VAI", "USDP", false, new[] { Symbol.Create("BTCUSDP", SecurityType.Crypto, Market.Binance), Symbol.Create("BTCVAI", SecurityType.Crypto, Market.Binance) } }, // No USDPVAI, but indirect conversion exists

            // BUSD Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "BUSD", false, null }, // No BUSDUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), Currencies.EUR, "BUSD", false, new[] { Symbol.Create("EURBUSD", SecurityType.Crypto, Market.Binance) } },
            new object[] { new BinanceBrokerageModel(), Currencies.GBP, "BUSD", false, new[] { Symbol.Create("GBPBUSD", SecurityType.Crypto, Market.Binance) } },

            // UST Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "UST", false, null }, // No USTUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), "VAI", "UST", false, new[] { Symbol.Create("BTCUST", SecurityType.Crypto, Market.Binance), Symbol.Create("BTCVAI", SecurityType.Crypto, Market.Binance) } }, // No USTVAI, but indirect conversion exists

            // TUSD Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "TUSD", false, null }, // No TUSDUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), "VAI", "TUSD", false, new[] { Symbol.Create("BTCTUSD", SecurityType.Crypto, Market.Binance), Symbol.Create("BTCVAI", SecurityType.Crypto, Market.Binance) } }, // No TUSDVAI, but indirect conversion exists

            // DAI Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "DAI", false, null }, // No DAIUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), Currencies.EUR, "DAI", false, new[] { Symbol.Create("BNBDAI", SecurityType.Crypto, Market.Binance), Symbol.Create("BNBEUR", SecurityType.Crypto, Market.Binance) } }, // No DAIEUR, but indirect conversion exists
            new object[] { new BinanceBrokerageModel(), Currencies.GBP, "DAI", false, new[] { Symbol.Create("BNBDAI", SecurityType.Crypto, Market.Binance), Symbol.Create("BNBGBP", SecurityType.Crypto, Market.Binance) } }, // No DAIGBP, but indirect conversion exists

            // USDS Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "SUSD", false, null }, // No SUSDUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), Currencies.EUR, "SUSD", false, new[] { Symbol.Create("SUSDBTC", SecurityType.Crypto, Market.Binance), Symbol.Create("BTCEUR", SecurityType.Crypto, Market.Binance) } }, // No SUSDEUR, but indirect conversion exists
            new object[] { new BinanceBrokerageModel(), Currencies.GBP, "SUSD", false, new[] { Symbol.Create("SUSDBTC", SecurityType.Crypto, Market.Binance), Symbol.Create("BTCGBP", SecurityType.Crypto, Market.Binance) } }, // No SUSDGBP, but indirect conversion exists

            // IDRT Cases
            new object[] { new BinanceBrokerageModel(), "IDR", "IDRT", false, null }, // No IDRTIDR, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), Currencies.EUR, "IDRT", false, new[] { Symbol.Create("BNBIDRT", SecurityType.Crypto, Market.Binance), Symbol.Create("BNBEUR", SecurityType.Crypto, Market.Binance) } }, // No IDRTEUR, but indirect conversion exists
            new object[] { new BinanceBrokerageModel(), Currencies.GBP, "IDRT", false, new[] { Symbol.Create("BNBIDRT", SecurityType.Crypto, Market.Binance), Symbol.Create("BNBGBP", SecurityType.Crypto, Market.Binance) } }, // No IDRTGBP, but indirect conversion exists

            new object[] { new OandaBrokerageModel(), Currencies.EUR, "INR", false, new[] { Symbol.Create("USDINR", SecurityType.Forex, Market.Oanda), Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda) } }, // No INREUR, but indirect conversion exists

            // FDUSD Cases
            new object[] { new BinanceBrokerageModel(), Currencies.USD, "FDUSD", false, null }, // No FDUSDUSD, but does not throw! Conversion 1-1
            new object[] { new BinanceBrokerageModel(), "VAI", "FDUSD", false, new[] { Symbol.Create("BTCFDUSD", SecurityType.Crypto, Market.Binance), Symbol.Create("BTCVAI", SecurityType.Crypto, Market.Binance) } }, // No FDUSDVAI, but indirect conversion exists
        };
    }
}
