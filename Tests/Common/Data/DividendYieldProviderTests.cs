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

using NUnit.Framework;
using QuantConnect.Data;
using System;
using System.Collections.Generic;
using System.Threading;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class DividendYieldProviderTests
    {
        // Without a price:
        [TestCase("19700306", null, 0.0)]           // Date in before the first date in file
        [TestCase("20191107", null, 0.0118177)]     // Dividend on this date
        [TestCase("20191108", null, 0.0118177)]     // Same dividend yield is fill-forwarded for every day until next dividend
        [TestCase("20200205", null, 0.0118177)]
        [TestCase("20200206", null, 0.0118177)]
        [TestCase("20200207", null, 0.0094708)]     // Dividend on this date
        [TestCase("20200208", null, 0.0094708)]
        [TestCase("20210203", null, 0.0067840)]
        [TestCase("20210204", null, 0.0067840)]
        [TestCase("20210205", null, 0.0059684)]     // Dividend on this date
        [TestCase("20210208", null, 0.0059684)]
        [TestCase("20210209", null, 0.0059684)]
        [TestCase("20491231", null, 0.0059684)]     // Date in far future, assuming same rate
        // With price:
        [TestCase("19700306", 1.0, 0.0)]            // Date in before the first date in file
        [TestCase("20191107", 257.24, 0.0118177)]   // Dividend on this date
        [TestCase("20191108", 259.43, 0.0117179)]
        [TestCase("20200205", 318.85, 0.0095342)]
        [TestCase("20200206", 321.45, 0.0094571)]
        [TestCase("20200207", 325.21, 0.0094708)]   // Dividend on this date
        [TestCase("20200210", 320.03, 0.0096240)]
        [TestCase("20210203", 134.99, 0.0059819)]
        [TestCase("20210204", 133.94, 0.0060288)]
        [TestCase("20210205", 137.39, 0.0059684)]   // Dividend on this date
        [TestCase("20210208", 136.76, 0.0059959)]
        [TestCase("20210209", 136.91, 0.0059893)]   // Date in far future, assuming same rate
        public void GetDividendYieldRate(string dateString, double? price, double expected)
        {
            var symbol = Symbols.AAPL;
            var provider = new DividendYieldProvider(symbol);
            var dateTime = Parse.DateTimeExact(dateString, "yyyyMMdd");
            var result = price.HasValue
                ? provider.GetDividendYield(dateTime, Convert.ToDecimal(price.Value))
                : provider.GetDividendYield(dateTime);

            Assert.AreEqual(expected, (double)result, 1e-7);
        }

        [TestCase("19700101", 0.0)]   // Date before Time.Start
        [TestCase("20200101", 0.0)]
        [TestCase("20500101", 0.0)]   // Date in far future
        public void GetDividendYieldWithoutFactorFile(string dateString, decimal expected)
        {
            var symbol = Symbols.EURUSD;
            var provider = new DividendYieldProvider(symbol);
            var dateTime = Parse.DateTimeExact(dateString, "yyyyMMdd");
            var result = provider.GetDividendYield(dateTime);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void CacheIsCleared()
        {
            var symbol = Symbols.AAPL;
            using var fileProviderTest = new DividendYieldProviderTest(symbol);

            fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            var fetchCount = fileProviderTest.FetchCount;
            Thread.Sleep(1);
            fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.AreEqual(fetchCount, fileProviderTest.FetchCount);

            var counter = 0;
            while (counter++ < 10)
            {
                fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
                if (fileProviderTest.FetchCount <= fetchCount)
                {
                    Thread.Sleep(250);
                }
                else
                {
                    break;
                }
            }
            Assert.Less(counter, 10);
        }

        [Test]
        public void AnotherSymbolCall()
        {
            using var fileProviderTest = new DividendYieldProviderTest(Symbol.Create("TEST_A", SecurityType.Equity, QuantConnect.Market.USA));

            var applYield = fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.AreEqual(1, fileProviderTest.FetchCount);

            using var fileProviderTest2 = new DividendYieldProviderTest(Symbol.Create("TEST_B", SecurityType.Equity, QuantConnect.Market.USA));

            var spyYield = fileProviderTest2.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.AreEqual(1, fileProviderTest2.FetchCount);
        }

        private class DividendYieldProviderTest : DividendYieldProvider, IDisposable
        {
            public int FetchCount { get; set; }

            protected override TimeSpan CacheRefreshPeriod => TimeSpan.FromSeconds(1);

            public DividendYieldProviderTest(Symbol symbol)
                : base(symbol)
            {
            }

            protected override List<BaseData> LoadCorporateEvents(Symbol symbol)
            {
                FetchCount++;
                return base.LoadCorporateEvents(symbol);
            }

            public void Reset()
            {
                try
                {
                    // stop the refresh task
                    var task = DividendYieldProvider._cacheClearTask;
                    DividendYieldProvider._cacheClearTask = null;
                    task.Dispose();
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                Reset();
            }
        }
    }
}
