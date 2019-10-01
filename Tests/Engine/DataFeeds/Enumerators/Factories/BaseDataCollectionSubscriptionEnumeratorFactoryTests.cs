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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class BaseDataCollectionSubscriptionEnumeratorFactoryTests
    {
        // This test reports higher memory usage when ran with Travis, so we exclude it for now
        [Test, Category("TravisExclude")]
        public void DoesNotLeakMemory()
        {
            var symbol = CoarseFundamental.CreateUniverseSymbol(Market.USA);
            var config = new SubscriptionDataConfig(typeof(CoarseFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var universeSettings = new UniverseSettings(Resolution.Daily, 2m, true, false, TimeSpan.FromDays(1));
            var securityInitializer = new BrokerageModelSecurityInitializer(new DefaultBrokerageModel(), SecuritySeeder.Null);
            var universe = new CoarseFundamentalUniverse(universeSettings, securityInitializer, x => new List<Symbol>{ Symbols.AAPL });

            var fileProvider = new DefaultDataProvider();

            var factory = new BaseDataCollectionSubscriptionEnumeratorFactory();

            GC.Collect();
            var ramUsageBeforeLoop = OS.TotalPhysicalMemoryUsed;

            var date = new DateTime(2014, 3, 25);

            const int iterations = 1000;
            for (var i = 0; i < iterations; i++)
            {
                var request = new SubscriptionRequest(true, universe, security, config, date, date);
                using (var enumerator = factory.CreateEnumerator(request, fileProvider))
                {
                    enumerator.MoveNext();
                }
            }

            GC.Collect();
            var ramUsageAfterLoop = OS.TotalPhysicalMemoryUsed;

            Log.Trace($"RAM usage - before: {ramUsageBeforeLoop} MB, after: {ramUsageAfterLoop} MB");

            Assert.IsTrue(ramUsageAfterLoop - ramUsageBeforeLoop < 10);
        }

        [Test]
        public void ReturnsExpectedTimestamps()
        {
            var symbol = CoarseFundamental.CreateUniverseSymbol(Market.USA);
            var config = new SubscriptionDataConfig(typeof(CoarseFundamental), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            var universeSettings = new UniverseSettings(Resolution.Daily, 2m, true, false, TimeSpan.FromDays(1));
            var securityInitializer = new BrokerageModelSecurityInitializer(new DefaultBrokerageModel(), SecuritySeeder.Null);
            var universe = new CoarseFundamentalUniverse(universeSettings, securityInitializer, x => new List<Symbol> { Symbols.AAPL });

            var fileProvider = new DefaultDataProvider();

            var factory = new BaseDataCollectionSubscriptionEnumeratorFactory();

            var dateStart = new DateTime(2014, 3, 26);
            var dateEnd = new DateTime(2014, 3, 27);
            var days = (dateEnd - dateStart).Days + 1;

            var request = new SubscriptionRequest(true, universe, security, config, dateStart, dateEnd);

            using (var enumerator = factory.CreateEnumerator(request, fileProvider))
            {
                dateStart = dateStart.AddDays(-1);
                for (var i = 0; i <= days; i++)
                {
                    Assert.IsTrue(enumerator.MoveNext());

                    var current = enumerator.Current as BaseDataCollection;
                    Assert.IsNotNull(current);
                    Assert.AreEqual(dateStart.AddDays(i), current.Time);
                    Assert.AreEqual(dateStart.AddDays(i), current.EndTime);
                    Assert.AreEqual(dateStart.AddDays(i - 1), current.Data[0].Time);
                    Assert.AreEqual(dateStart.AddDays(i), current.Data[0].EndTime);
                }

                Assert.IsFalse(enumerator.MoveNext());
                Assert.IsNotNull(enumerator.Current);
            }
        }
    }
}
