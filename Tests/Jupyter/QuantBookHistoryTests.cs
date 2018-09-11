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
using Python.Runtime;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Jupyter
{
    [TestFixture]
    public class QuantBookHistoryTests
    {
        dynamic _module;

        [SetUp]
        public void Setup()
        {
            SymbolCache.Clear();

            using (Py.GIL())
            {
                _module = Py.Import("Test_QuantBookHistory");
            }
        }

        [Test]
        [TestCase(2013, 10, 11, SecurityType.Equity, "SPY")]
        [TestCase(2014, 5, 9, SecurityType.Forex, "EURUSD")]
        [TestCase(2016, 10, 9, SecurityType.Crypto, "BTCUSD")]
        public void SecurityQuantBookHistoryTests(int year, int month, int day, SecurityType securityType, string symbol)
        {
            var startDate = new DateTime(year, month, day);
            var securityTestHistory = _module.SecurityHistoryTest(startDate, securityType, symbol);

            // Get the last 10 candles
            var periodHistory = securityTestHistory.test_period_overload(10);
            var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
            Assert.AreEqual(10, count);

            // Get the one day of data
            var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(1));
            var firstIndex = (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);

            // Get the one day of data, ending one day before start date
            var startEndHistory = securityTestHistory.test_daterange_overload(startDate.AddDays(-1));
            firstIndex = (DateTime)(startEndHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.GreaterOrEqual(startDate.AddDays(-2).Date, firstIndex.Date);
        }

        [Test]
        [TestCase(2014, 5, 9, "Nifty", "NIFTY")]
        public void CustomDataQuantBookHistoryTests(int year, int month, int day, string customDataType, string symbol)
        {
            var startDate = new DateTime(year, month, day);
            var securityTestHistory = _module.CustomDataHistoryTest(startDate, customDataType, symbol);

            // Get the last 5 candles
            var periodHistory = securityTestHistory.test_period_overload(5);
            var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
            Assert.AreEqual(5, count);

            // Get the one day of data
            var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(8));
            var firstIndex = (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.AreEqual(startDate.AddDays(-7), firstIndex);

            // Get the one day of data, ending one day before start date
            var startEndHistory = securityTestHistory.test_daterange_overload(startDate.AddDays(-2));
            firstIndex = (DateTime)(startEndHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.AreEqual(startDate.AddDays(-2).Date, firstIndex.Date);
        }

        [Test]
        public void MultipleSecuritiesQuantBookHistoryTests()
        {
            var startDate = new DateTime(2014, 5, 9);
            var securityTestHistory = _module.MultipleSecuritiesHistoryTest(startDate, null, null);

            // Get the last 5 candles
            var periodHistory = securityTestHistory.test_period_overload(5);
            var count = (periodHistory.shape[0] as PyObject).AsManagedObject(typeof(int));
            Assert.AreEqual(6, count);

            // Get the one day of data
            var timedeltaHistory = securityTestHistory.test_period_overload(TimeSpan.FromDays(8));
            var firstIndex = (DateTime)(timedeltaHistory.index.values[0] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.AreEqual(startDate.AddDays(-8), firstIndex);
        }

        [Test]
        public void OptionQuantBookHistoryTests()
        {
            var symbol = "TWX";
            var startDate = new DateTime(2014, 6, 6);
            var securityTestHistory = _module.OptionHistoryTest(startDate, SecurityType.Option, symbol);

            // Get the one day of data, ending on start date
            var startEndHistory = securityTestHistory.test_daterange_overload(startDate);
            var firstIndex = (DateTime)(startEndHistory.index.values[0][4] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);
        }

        [Test]
        public void FutureQuantBookHistoryTests()
        {
            var symbol = Futures.Indices.SP500EMini;
            var startDate = new DateTime(2013, 10, 11);
            var securityTestHistory = _module.FutureHistoryTest(startDate, SecurityType.Future, symbol);

            // Get the one day of data, ending on start date
            var startEndHistory = securityTestHistory.test_daterange_overload(startDate);
            var firstIndex = (DateTime)(startEndHistory.index.values[0][2] as PyObject).AsManagedObject(typeof(DateTime));
            Assert.GreaterOrEqual(startDate.AddDays(-1).Date, firstIndex.Date);
        }
    }
}
