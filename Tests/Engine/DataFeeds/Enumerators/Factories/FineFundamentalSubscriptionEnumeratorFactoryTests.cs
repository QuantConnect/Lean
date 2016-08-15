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
using System.Diagnostics;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators.Factories
{
    [TestFixture, Category("TravisExclude")]
    public class FineFundamentalSubscriptionEnumeratorFactoryTests
    {
        [Test]
        public void ReadsFineFundamentalCorrectly()
        {
            var stopwatch = Stopwatch.StartNew();
            var totalRows = 0;

            var start = new DateTime(2015, 1, 1);
            var end = new DateTime(2015, 12, 31);
            var config = new SubscriptionDataConfig(typeof(FineFundamental), Symbols.AAPL, Resolution.Daily, TimeZones.NewYork, TimeZones.NewYork, false, false, false, false, TickType.Trade, false);
            var security = new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork), config, new Cash(CashBook.AccountCurrency, 0, 1), SymbolProperties.GetDefault(CashBook.AccountCurrency));
            var request = new SubscriptionRequest(false, null, security, config, start, end);

            var factory = new FineFundamentalSubscriptionEnumeratorFactory();
            var enumerator = factory.CreateEnumerator(request);
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current as FineFundamental;
                if (current == null) continue;

                totalRows++;
            }
 
            stopwatch.Stop();
            Console.WriteLine("Total rows: {0}, elapsed time: {1}", totalRows, stopwatch.Elapsed);
        }
    }
}
