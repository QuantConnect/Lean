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
        [TestCase("19700306", 0.0)]   // Date in before the first date in file
        [TestCase("20200205", 0.04147)]
        [TestCase("20200206", 0.03355)]
        [TestCase("20200207", 0.03355)]
        [TestCase("20210203", 0.01676)]
        [TestCase("20210204", 0.01239)]
        [TestCase("20210205", 0.01239)]
        [TestCase("20491231", 0.01239)]   // Date in far future, assuming same rate
        public void GetDividendYieldRate(string dateString, double expected)
        {
            var symbol = Symbols.AAPL;
            var provider = new DividendYieldProvider(symbol);
            var dateTime = Parse.DateTimeExact(dateString, "yyyyMMdd");
            var result = provider.GetDividendYield(dateTime);

            Assert.AreEqual(expected, (double)result, 0.0001d);
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
            fileProviderTest.Reset();

            fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            var fetchCount = fileProviderTest.FetchCount;
            Thread.Sleep(1);
            fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.AreEqual(fetchCount, fileProviderTest.FetchCount);

            Thread.Sleep(TimeSpan.FromSeconds(10));

            fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.Greater(fileProviderTest.FetchCount, fetchCount);
        }

        [Test]
        public void AnotherSymbolCall()
        {
            using var fileProviderTest = new DividendYieldProviderTest(Symbols.AAPL);

            var applYield = fileProviderTest.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.AreEqual(1, fileProviderTest.FetchCount);

            using var fileProviderTest2 = new DividendYieldProviderTest(Symbols.SPY);

            var spyYield = fileProviderTest2.GetDividendYield(new DateTime(2020, 1, 1));
            Assert.AreEqual(1, fileProviderTest2.FetchCount);

            Assert.AreNotEqual(applYield, spyYield);
        }

        private class DividendYieldProviderTest : DividendYieldProvider, IDisposable
        {
            public int FetchCount { get; set; }

            protected override TimeSpan CacheRefreshPeriod => TimeSpan.FromSeconds(5);

            public DividendYieldProviderTest(Symbol symbol)
                : base(symbol) 
            { 
            }

            protected override Dictionary<DateTime, decimal> LoadDividendYieldProvider(Symbol symbol)
            {
                FetchCount++;
                return base.LoadDividendYieldProvider(symbol);
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
