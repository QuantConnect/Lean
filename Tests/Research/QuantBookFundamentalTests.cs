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
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Research;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantBookFundamentalTests
    {
        private dynamic _module;
        private DateTime _startDate;
        private DateTime _endDate;
        private ILogHandler _logHandler;
        private QuantBook _qb;

        [OneTimeSetUp]
        public void Setup()
        {
            // Store initial handler
            _logHandler = Log.LogHandler;

            SymbolCache.Clear();
            MarketHoursDatabase.Reset();

            // Using a date that we have data for in the repo
            _startDate = new DateTime(2014, 3, 31);
            _endDate = new DateTime(2014, 3, 31);

            // Our qb instance to test on
            _qb = new QuantBook();

            using (Py.GIL())
            {
                _module = Py.Import("Test_QuantBookHistory");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Reset to initial handler
            Log.LogHandler = _logHandler;
        }

        [Test]
        public void DefaultEndDate()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-7);

            // Expected end date should be either today if tradable, or last tradable day
            var aapl = _qb.AddEquity("AAPL");
            var now = DateTime.UtcNow.Date;
            var expectedDate = aapl.Exchange.Hours.IsDateOpen(now) ? now : aapl.Exchange.Hours.GetPreviousTradingDay(now);

            IEnumerable<DataDictionary<dynamic>> data = _qb.GetFundamental("AAPL", "ValuationRatios.PERatio", startDate);

            // Check that the last day in the collection is as expected
            var lastDay = data.Last();
            Assert.AreEqual(expectedDate, lastDay.Time);
        }

        [TestCaseSource(nameof(DataTestCases))]
        public void PyFundamentalData(dynamic input)
        {
            using (Py.GIL())
            {
                var testModule = _module.FundamentalHistoryTest();
                var dataFrame = testModule.getFundamentals(input[0], input[1], _startDate, _endDate);

                // Should not be empty
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                // Get the test row
                var testRow = dataFrame.loc[_startDate.ToPython()];
                Assert.IsFalse(testRow.empty.AsManagedObject(typeof(bool)));

                // Check the length
                var count = testRow.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 1);

                // Verify the data value
                var index = testRow.index[0];
                var value = testRow.at[index].AsManagedObject(input[2].GetType());
                Assert.AreEqual(input[2], value);
            }
        }

        [TestCaseSource(nameof(DataTestCases))]
        public void CSharpFundamentalData(dynamic input)
        {
            var data = _qb.GetFundamental(input[0], input[1], _startDate, _endDate);

            foreach (var day in data)
            {
                foreach (var value in day.Values)
                {
                    Assert.AreEqual(input[2], value);
                    Assert.AreEqual(_startDate, day.Time);
                }
            }
        }

        [TestCaseSource(nameof(NullRequestTestCases))]
        public void PyReturnNullTest(dynamic input)
        {
            using (Py.GIL())
            {
                var testModule = _module.FundamentalHistoryTest();
                var data = testModule.getFundamentals(input[0], input[1], input[2], input[3]);
                Assert.IsEmpty(data);
            }
        }

        [TestCaseSource(nameof(NullRequestTestCases))]
        public void CSharpReturnNullTest(dynamic input)
        {
            var data = _qb.GetFundamental(input[0], input[1], input[2], input[3]);
            Assert.IsEmpty(data);
        }

        // Different requests and their expected values
        private static readonly object[] DataTestCases =
        {
            new object[] {new List<string> {"AAPL"}, "ValuationRatios.PERatio", 13.2725m},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios.BookValuePerShare", 22.5177},
            new object[] {new List<Symbol> {Symbol.Create("AIG", SecurityType.Equity, Market.USA)}, "FinancialStatements.NumberOfShareHolders", 36319}
        };

        // Different requests that should return null
        // Nonexistent data; start date after end date;
        private static readonly object[] NullRequestTestCases =
        {
            new object[] {Symbol.Create("AIG", SecurityType.Equity, Market.USA), "ValuationRatios.PERatio", new DateTime(1972, 4, 1),  new DateTime(1972, 4, 1)},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios.BookValuePerShare", new DateTime(2014, 4, 1), new DateTime(2014, 3, 31)},
        };
    }
}

