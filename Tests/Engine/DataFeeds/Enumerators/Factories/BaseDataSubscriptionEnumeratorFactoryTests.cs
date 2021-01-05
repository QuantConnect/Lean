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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture]
    public class BaseDataSubscriptionEnumeratorFactoryTests
    {
        // This test reports higher memory usage when ran with Travis, so we exclude it for now
        [Test, Category("TravisExclude")]
        public void DoesNotLeakMemory()
        {
            var symbol = Symbols.AAPL;
            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            var mapFileProvider = new LocalDiskMapFileProvider();
            var factorFileProvider = new LocalDiskFactorFileProvider(mapFileProvider);
            var mapFileResolver = mapFileProvider.Get(security.Symbol.ID.Market);
            var fileProvider = new DefaultDataProvider();

            var factory = new BaseDataSubscriptionEnumeratorFactory(false, mapFileResolver, factorFileProvider);

            GC.Collect();
            var ramUsageBeforeLoop = OS.TotalPhysicalMemoryUsed;

            var date = new DateTime(1998, 1, 1);

            const int iterations = 1000;
            for (var i = 0; i < iterations; i++)
            {
                var request = new SubscriptionRequest(false, null, security, config, date, date);
                using (var enumerator = factory.CreateEnumerator(request, fileProvider))
                {
                    enumerator.MoveNext();
                }
                date = date.AddDays(1);
            }

            GC.Collect();
            var ramUsageAfterLoop = OS.TotalPhysicalMemoryUsed;

            Log.Trace($"RAM usage - before: {ramUsageBeforeLoop} MB, after: {ramUsageAfterLoop} MB");

            Assert.IsTrue(ramUsageAfterLoop - ramUsageBeforeLoop < 10);
        }

    }
}
