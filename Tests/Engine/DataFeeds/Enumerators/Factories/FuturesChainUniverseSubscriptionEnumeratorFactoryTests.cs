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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class FuturesChainUniverseSubscriptionEnumeratorFactoryTests
    {
        [Test]
        public void RefreshesFutureChainUniverseOnDateChange()
        {
            var startTime = new DateTime(2018, 10, 17, 10, 0, 0);
            var timeProvider = new ManualTimeProvider(startTime);

            var symbolUniverse = new TestDataQueueUniverseProvider(timeProvider);
            var factory = new FuturesChainUniverseSubscriptionEnumeratorFactory(symbolUniverse, timeProvider);

            var canonicalSymbol = Symbol.Create("VX", SecurityType.Future, Market.USA, "/VX");

            var quoteCurrency = new Cash(Currencies.USD, 0, 1);
            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, canonicalSymbol, SecurityType.Future);
            var config = new SubscriptionDataConfig(
                typeof(ZipEntryName),
                canonicalSymbol,
                Resolution.Minute,
                TimeZones.Utc,
                TimeZones.Chicago,
                true,
                false,
                false,
                false,
                TickType.Quote,
                false,
                DataNormalizationMode.Raw
            );

            var future = new Future(
                canonicalSymbol,
                exchangeHours,
                quoteCurrency,
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var universeSettings = new UniverseSettings(Resolution.Minute, 0, true, false, TimeSpan.Zero);
            var universe = new FuturesChainUniverse(future, universeSettings);
            var request = new SubscriptionRequest(true, universe, future, config, startTime, Time.EndOfTime);
            var enumerator = factory.CreateEnumerator(request, new DefaultDataProvider());

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);
            var data = enumerator.Current as FuturesChainUniverseDataCollection;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Data.Count);

            timeProvider.Advance(Time.OneSecond);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);

            timeProvider.Advance(Time.OneMinute);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.AreEqual(1, symbolUniverse.TotalLookupCalls);

            timeProvider.Advance(Time.OneDay);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNotNull(enumerator.Current);
            Assert.AreEqual(2, symbolUniverse.TotalLookupCalls);
            data = enumerator.Current as FuturesChainUniverseDataCollection;
            Assert.IsNotNull(data);
            Assert.AreEqual(2, data.Data.Count);

            timeProvider.Advance(Time.OneMinute);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsNull(enumerator.Current);
            Assert.AreEqual(2, symbolUniverse.TotalLookupCalls);

            enumerator.Dispose();
        }

        public class TestDataQueueUniverseProvider : IDataQueueUniverseProvider
        {
            private readonly Symbol[] _symbolList1 =
            {
                Symbol.CreateFuture("VX", Market.USA, new DateTime(2018, 10, 31))
            };
            private readonly Symbol[] _symbolList2 =
            {
                Symbol.CreateFuture("VX", Market.USA, new DateTime(2018, 10, 31)),
                Symbol.CreateFuture("VX", Market.USA, new DateTime(2018, 11, 30)),
            };

            private readonly ITimeProvider _timeProvider;

            public int TotalLookupCalls { get; set; }

            public TestDataQueueUniverseProvider(ITimeProvider timeProvider)
            {
                _timeProvider = timeProvider;
            }

            public IEnumerable<Symbol> LookupSymbols(string lookupName, SecurityType securityType, string securityCurrency = null, string securityExchange = null)
            {
                TotalLookupCalls++;

                return _timeProvider.GetUtcNow().Date.Day >= 18 ? _symbolList2 : _symbolList1;
            }
        }
    }
}
