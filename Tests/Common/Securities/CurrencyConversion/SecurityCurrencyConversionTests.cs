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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.CurrencyConversion;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.CurrencyConversion
{
    [TestFixture]
    public class SecurityCurrencyConversionTests
    {
        [Test]
        public void LinearSearchFindsOneLegConversions()
        {
            var existingSecurities = new List<Security>(0);
            var potentialSymbols = new List<Symbol> { Symbols.EURUSD };

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub();
            subscriptions.SetDataManager(dataManager);

            var createdSecurities = new List<Security>();
            var makeNewSecurity = new Func<Symbol, Security>(symbol =>
            {
                var security = CreateSecurity(symbol);
                createdSecurities.Add(security);
                return security;
            });

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "EUR",
                "USD",
                existingSecurities,
                potentialSymbols,
                makeNewSecurity);

            var securities = currencyConversion.ConversionRateSecurities.ToList();
            Assert.AreEqual(1, securities.Count);
            Assert.AreEqual(createdSecurities, securities);
            Assert.AreEqual(Symbols.EURUSD, securities[0].Symbol);
        }

        [Test]
        public void LinearSearchFindsTwoLegConversions()
        {
            var existingSecurities = new List<Security>(0);
            var potentialSymbols = new List<Symbol> { Symbols.BTCUSD, Symbols.EURUSD };

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub();
            subscriptions.SetDataManager(dataManager);

            var createdSecurities = new List<Security>();
            var makeNewSecurity = new Func<Symbol, Security>(symbol =>
            {
                var security = CreateSecurity(symbol);
                createdSecurities.Add(security);
                return security;
            });

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "BTC",
                "EUR",
                existingSecurities,
                potentialSymbols,
                makeNewSecurity);

            var securities = currencyConversion.ConversionRateSecurities.ToList();
            Assert.AreEqual(2, securities.Count);
            Assert.AreEqual(createdSecurities, securities);
            Assert.AreEqual(Symbols.BTCUSD, securities[0].Symbol);
            Assert.AreEqual(Symbols.EURUSD, securities[1].Symbol);
        }

        [Test]
        public void LinearSearchPrefersExistingSecuritiesOverNewOnesOneLeg()
        {
            var existingSecurities = new List<Security> { CreateSecurity(Symbols.EURUSD) };
            var potentialSymbols = new List<Symbol> { Symbols.EURUSD };

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub();
            subscriptions.SetDataManager(dataManager);

            var createdSecurities = new List<Security>();
            var makeNewSecurity = new Func<Symbol, Security>(symbol =>
            {
                var security = CreateSecurity(symbol);
                createdSecurities.Add(security);
                return security;
            });

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "EUR",
                "USD",
                existingSecurities,
                potentialSymbols,
                makeNewSecurity);

            var securities = currencyConversion.ConversionRateSecurities.ToList();
            Assert.AreEqual(1, securities.Count);
            Assert.AreEqual(0, createdSecurities.Count);
            Assert.AreEqual(existingSecurities, securities);
        }

        [Test]
        public void LinearSearchPrefersExistingSecuritiesOverNewOnesTwoLeg()
        {
            var existingSecurities = new List<Security> { CreateSecurity(Symbols.BTCUSD) };
            var potentialSymbols = new List<Symbol> { Symbols.BTCUSD, Symbols.EURUSD };

            var subscriptions = new SubscriptionManager(NullTimeKeeper.Instance);
            var dataManager = new DataManagerStub();
            subscriptions.SetDataManager(dataManager);

            var createdSecurities = new List<Security>();
            var makeNewSecurity = new Func<Symbol, Security>(symbol =>
            {
                var security = CreateSecurity(symbol);
                createdSecurities.Add(security);
                return security;
            });

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "BTC",
                "EUR",
                existingSecurities,
                potentialSymbols,
                makeNewSecurity);

            var securities = currencyConversion.ConversionRateSecurities.ToList();
            Assert.AreEqual(2, securities.Count);
            Assert.AreEqual(existingSecurities[0], securities[0]);
            Assert.AreEqual(createdSecurities[0], securities[1]);
            Assert.AreEqual(Symbols.EURUSD, securities[1].Symbol);
        }

        [Test]
        public void LinearSearchThrowsWhenNoConversionPossible()
        {
            var existingSecurities = new List<Security>(0);
            var potentialSymbols = new List<Symbol> { Symbols.EURGBP };

            Assert.Throws<ArgumentException>(() => SecurityCurrencyConversion.LinearSearch(
                "EUR",
                "USD",
                existingSecurities,
                potentialSymbols,
                CreateSecurity));
        }

        [TestCaseSource(nameof(oneLegCases))]
        public void UpdateCalculatesNewConversionRateOneLeg(
            string sourceCurrency,
            string destinationCurrency,
            Symbol symbol,
            decimal expectedRate)
        {
            var existingSecurities = new List<Security> { CreateSecurity(symbol) };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                sourceCurrency,
                destinationCurrency,
                existingSecurities,
                new List<Symbol>(0),
                CreateSecurity);

            existingSecurities[0].SetMarketPrice(new Tick { Value = 10m });

            currencyConversion.Update();
            Assert.AreEqual(expectedRate, currencyConversion.ConversionRate);
        }

        [TestCaseSource(nameof(twoLegCases))]
        public void UpdateCalculatesNewConversionRateTwoLeg(
            string sourceCurrency,
            string destinationCurrency,
            Symbol symbol1,
            Symbol symbol2,
            decimal expectedRate)
        {
            var existingSecurities = new List<Security>
            {
                CreateSecurity(symbol1),
                CreateSecurity(symbol2)
            };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                sourceCurrency,
                destinationCurrency,
                existingSecurities,
                new List<Symbol>(0),
                CreateSecurity);

            existingSecurities[0].SetMarketPrice(new Tick { Value = 15m });
            existingSecurities[1].SetMarketPrice(new Tick { Value = 25m });

            currencyConversion.Update();
            Assert.AreEqual(expectedRate, currencyConversion.ConversionRate);
        }

        [Test]
        public void UpdateReturnsZeroWhenNoData()
        {
            var existingSecurities = new List<Security> { CreateSecurity(Symbols.BTCUSD) };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "BTC",
                "USD",
                existingSecurities,
                new List<Symbol>(0),
                CreateSecurity);

            currencyConversion.Update();
            Assert.AreEqual(0m, currencyConversion.ConversionRate);
        }

        [Test]
        public void UpdateReturnsZeroWhenNoDataForOneOfTwoSymbols()
        {
            var existingSecurities = new List<Security>
            {
                CreateSecurity(Symbols.ETHBTC),
                CreateSecurity(Symbols.BTCUSD)
            };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "ETH",
                "USD",
                existingSecurities,
                new List<Symbol>(0),
                CreateSecurity);

            existingSecurities[0].SetMarketPrice(new Tick { Value = 15m });

            currencyConversion.Update();
            Assert.AreEqual(0m, currencyConversion.ConversionRate);
        }

        [Test]
        public void ConversionRateReturnsLatestConversionRate()
        {
            var existingSecurities = new List<Security> { CreateSecurity(Symbols.BTCUSD) };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "BTC",
                "USD",
                existingSecurities,
                new List<Symbol>(0),
                CreateSecurity);

            Assert.AreEqual(0m, currencyConversion.ConversionRate);

            existingSecurities[0].SetMarketPrice(new Tick { Value = 10m });
            currencyConversion.Update();
            Assert.AreEqual(10m, currencyConversion.ConversionRate);

            existingSecurities[0].SetMarketPrice(new Tick { Value = 20m });
            currencyConversion.Update();
            Assert.AreEqual(20m, currencyConversion.ConversionRate);
        }

        [Test]
        public void ConversionRateZeroAtStart()
        {
            var existingSecurities = new List<Security>(0);
            var potentialSymbols = new List<Symbol> { Symbols.EURUSD };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "EUR",
                "USD",
                existingSecurities,
                potentialSymbols,
                CreateSecurity);

            Assert.AreEqual(0, currencyConversion.ConversionRate);
        }

        [Test]
        public void SourceCurrencyReturnsCorrectValue()
        {
            var existingSecurities = new List<Security>(0);
            var potentialSymbols = new List<Symbol> { Symbols.EURUSD };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "EUR",
                "USD",
                existingSecurities,
                potentialSymbols,
                CreateSecurity);

            Assert.AreEqual("EUR", currencyConversion.SourceCurrency);
        }

        [Test]
        public void DestinationCurrencyReturnsCorrectValue()
        {
            var existingSecurities = new List<Security>(0);
            var potentialSymbols = new List<Symbol> { Symbols.EURUSD };

            var currencyConversion = SecurityCurrencyConversion.LinearSearch(
                "EUR",
                "USD",
                existingSecurities,
                potentialSymbols,
                CreateSecurity);

            Assert.AreEqual("USD", currencyConversion.DestinationCurrency);
        }

        private static Security CreateSecurity(Symbol symbol)
        {
            var timezone = TimeZones.NewYork;

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                symbol,
                Resolution.Hour,
                timezone,
                timezone,
                true,
                false,
                false);

            return new Security(
                SecurityExchangeHours.AlwaysOpen(timezone),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        /// <summary>
        /// One leg Update() test cases.
        /// sourceCurrency, destinationCurrency, symbol, expectedRate
        /// expectedRate is the expected rate when the value of symbol is set to 10
        /// </summary>
        private static object[] oneLegCases =
        {
            // Not inverted
            new object[] { "BTC", "USD", Symbols.BTCUSD, 10m },

            // Inverted
            new object[] { "USD", "BTC", Symbols.BTCUSD, 0.1m }
        };

        /// <summary>
        /// Two leg Update() test cases:
        /// sourceCurrency, destinationCurrency, symbol1, symbol2, expectedRate
        /// expectedRate is the expected rate when the value of symbol1 is set to 15 and the value of symbol2 to 25
        /// </summary>
        private static object[] twoLegCases =
        {
            // Not inverted
            new object[] { "ETH", "USD", Symbols.ETHBTC, Symbols.BTCUSD, 15m * 25m },

            // First pair inverted
            new object[] { "USD", "BTC", Symbols.ETHUSD, Symbols.ETHBTC, (1m / 15m) * 25m },

            // Second pair inverted
            new object[] { "ETH", "BTC", Symbols.ETHUSD, Symbols.BTCUSD, 15m * (1m / 25m) },

            // Both pairs inverted
            new object[] { "USD", "ETH", Symbols.BTCUSD, Symbols.ETHBTC, (1m / 15m) * (1m / 25m) }
        };
    }
}
