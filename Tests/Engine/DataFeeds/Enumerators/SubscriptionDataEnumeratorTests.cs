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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class SubscriptionDataEnumeratorTests
    {

        [TestCase(typeof(TradeBar), true)]
        [TestCase(typeof(OpenInterest), false)]
        [TestCase(typeof(QuoteBar), false)]
        public void EnumeratorEmitsAuxData(Type typeOfConfig, bool shouldReceiveAuxData)
        {
            var config = CreateConfig(Resolution.Hour, typeOfConfig);
            var security = GetSecurity(config);
            var time = new DateTime(2010, 1, 1);
            var tzOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, time, time.AddDays(1));


            // Make a aux data stream; for this testing case we will just use delisting data points
            var totalPoints = 8;
            var stream = Enumerable.Range(0, totalPoints).Select(x => new Delisting { Time = time.AddHours(x) }).GetEnumerator();
            var enumerator = new SubscriptionDataEnumerator(config, security.Exchange.Hours, tzOffsetProvider, stream, false, false);

            // Test our SubscriptionDataEnumerator to see if it emits the aux data
            int dataReceivedCount = 0;
            while (enumerator.MoveNext())
            {
                dataReceivedCount++;
                if (enumerator.Current != null && enumerator.Current.Data.DataType == MarketDataType.Auxiliary)
                {
                    Assert.IsTrue(shouldReceiveAuxData);
                }
            }

            // If it should receive aux data it should have emitted all points
            // otherwise none should have been emitted
            if (shouldReceiveAuxData)
            {
                Assert.AreEqual(totalPoints, dataReceivedCount);
            }
            else
            {
                Assert.AreEqual(0, dataReceivedCount);
            }
        }


        private static Security GetSecurity(SubscriptionDataConfig config)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private static SubscriptionDataConfig CreateConfig(Resolution resolution, Type type)
        {
            return new SubscriptionDataConfig(type, Symbols.SPY, resolution, TimeZones.NewYork, TimeZones.NewYork, true, true, false);
        }
    }
}
