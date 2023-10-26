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
using System.Linq;
using Python.Runtime;
using NUnit.Framework;
using QuantConnect.Logging;
using QuantConnect.Research;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Tests.Common.Data.Fundamental;
using QuantConnect.Configuration;

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

            Config.Set("fundamental-data-provider", "NullFundamentalDataProvider");

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
            var expectedEndDate = aapl.Exchange.Hours.IsDateOpen(now) ? now : aapl.Exchange.Hours.GetPreviousTradingDay(now);
            expectedEndDate = expectedEndDate.AddDays(1);

            IEnumerable<DataDictionary<dynamic>> data = _qb.GetFundamental("AAPL", "", startDate);

            // Check that the last day in the collection is as expected
            var lastDay = data.Last();
            Assert.AreEqual(expectedEndDate, lastDay.Time);
            Assert.AreEqual(expectedEndDate, lastDay[aapl.Symbol].EndTime);
        }

        [TestCaseSource(nameof(DataTestCases))]
        public void PyFundamentalData(dynamic input)
        {
            using (Py.GIL())
            {
                var testModule = _module.FundamentalHistoryTest();
                FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProvider(), false);
                var dataFrame = testModule.getFundamentals(input[0], input[1], _startDate, _endDate);

                // Should not be empty
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                // Get the test row (plus 1 day since data is time-stamped with the base data's end time)
                var testRow = dataFrame.loc[_startDate.AddDays(1).ToPython()];
                Assert.IsFalse(testRow.empty.AsManagedObject(typeof(bool)));

                // Check the length
                var count = testRow.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 1);

                // Verify the data value
                var index = testRow.index[0];
                if (input.Length == 4)
                {
                    var fine = testRow.at[index].AsManagedObject(typeof(FineFundamental));
                    Assert.AreEqual(input[2], input[3](fine));
                }
                else
                {
                    var value = testRow.at[index].AsManagedObject(input[2].GetType());
                    Assert.AreEqual(input[2], value);
                }
            }
        }

        [TestCaseSource(nameof(DataTestCases))]
        public void CSharpFundamentalData(dynamic input)
        {
            FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProvider(), false);
            var data = _qb.GetFundamental(input[0], input[1], _startDate, _endDate);
            var currentDate = _startDate;

            foreach (var day in data)
            {
                // plus 1 day since data is time-stamped with the base data's end time
                currentDate = currentDate.AddDays(1);

                foreach (var value in day.Values)
                {
                    if (input.Length == 4)
                    {
                        Assert.AreEqual(input[2], input[3](value));
                    }
                    else
                    {
                        Assert.AreEqual(input[2], value);
                    }
                    Assert.AreEqual(currentDate, day.Time);
                }
            }
        }

        [Test]
        public void PyReturnNoneTest()
        {
            using (Py.GIL())
            {
                var start = new DateTime(2023, 10, 10);
                var symbol = Symbol.Create("AIG", SecurityType.Equity, Market.USA);
                var testModule = _module.FundamentalHistoryTest();
                var data = testModule.getFundamentals(symbol, "ValuationRatios.PERatio", start, start.AddDays(5));
                Assert.AreNotEqual(true, (bool)data.empty);

                var subdataframe = data.loc[start.AddDays(1).ToPython()];
                PyObject result = subdataframe[symbol.ID.ToString()];
                Assert.IsNull(result.As<object>());
            }
        }

        [TestCaseSource(nameof(NullRequestTestCases))]
        public void PyReturnNullTest(dynamic input)
        {
            using (Py.GIL())
            {
                var testModule = _module.FundamentalHistoryTest();
                var data = testModule.getFundamentals(input[0], input[1], input[2], input[3]);
                Assert.AreEqual(true, (bool)data.empty);
            }
        }

        [TestCaseSource(nameof(NullRequestTestCases))]
        public void CSharpReturnNullTest(dynamic input)
        {
            var data = _qb.GetFundamental(input[0], input[1], input[2], input[3]);
            Assert.IsEmpty(data);
        }

        [TestCaseSource(nameof(FundamentalEndTimeTestCases))]
        public void FundamentalDataEndTime(DateTime startDate, DateTime endDate)
        {
            var originalQBEndDate = _qb.EndDate;
            _qb.SetEndDate(endDate);

            var security = _qb.AddEquity("AAPL");

            var history = _qb.History(Symbols.AAPL, startDate, endDate, Resolution.Daily).ToList();
            Assert.IsNotEmpty(history);

            var fundamental = (_qb.GetFundamental("AAPL", "", startDate, endDate) as IEnumerable<DataDictionary<dynamic>>).ToList();

            var isEndDateOpen = security.Exchange.Hours.IsDateOpen(endDate);
            var expectedFundamentalCount = 10;
            var expectedHistoryCount = isEndDateOpen ? expectedFundamentalCount - 1 : expectedFundamentalCount;

            Assert.AreEqual(expectedFundamentalCount, fundamental.Count);
            Assert.AreEqual(expectedHistoryCount, history.Count);

            var historyTimes = history.Select(x => x.EndTime);
            var fundamentalTimes = fundamental.Select(x => x.Time).SkipLast(isEndDateOpen ? 1 : 0);

            CollectionAssert.AreEqual(historyTimes, fundamentalTimes);

            Assert.IsTrue(fundamental.All(x => x.Time == x.Values.Cast<FineFundamental>().Single().EndTime));

            _qb.RemoveSecurity(security.Symbol);
            _qb.SetEndDate(originalQBEndDate);
        }

        // Different requests and their expected values
        private static readonly object[] DataTestCases =
        {
            new object[] {new List<string> {"AAPL"}, null, 13.2725m, new Func<FineFundamental, double>(fundamental => fundamental.ValuationRatios.PERatio) },
            new object[] {new List<string> {"AAPL"}, "ValuationRatios.PERatio", 13.2725m},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios.BookValuePerShare", 22.5177},
            new object[] {new List<Symbol> {Symbol.Create("AIG", SecurityType.Equity, Market.USA)}, "FinancialStatements.NumberOfShareHolders.Value", 36319}
        };

        // Different requests that should return null
        // Nonexistent data; start date after end date;
        private static readonly object[] NullRequestTestCases =
        {
            new object[] {Symbol.Create("AIG", SecurityType.Equity, Market.USA), "ValuationRatios.PERatio", new DateTime(1972, 4, 1),  new DateTime(1972, 4, 1)},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios.BookValuePerShare", new DateTime(2014, 4, 1), new DateTime(2014, 3, 31)},
        };

        private static readonly TestCaseData[] FundamentalEndTimeTestCases =
        {
            // monday,friday
            new TestCaseData(new DateTime(2014, 3, 31), new DateTime(2014, 4, 11)),
            // monday,saturday
            new TestCaseData(new DateTime(2014, 3, 31), new DateTime(2014, 4, 12))
        };

        private class TestFundamentalDataProvider : IFundamentalDataProvider
        {
            public T Get<T>(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty name)
            {
                if (securityIdentifier == SecurityIdentifier.Empty)
                {
                    return default;
                }
                return Get(time, securityIdentifier, name);
            }

            private dynamic Get(DateTime time, SecurityIdentifier securityIdentifier, FundamentalProperty enumName)
            {
                var name = Enum.GetName(enumName);
                switch (name)
                {
                    case "ValuationRatios_PERatio":
                        return 13.2725d;
                    case "ValuationRatios_BookValuePerShare":
                        return 22.5177d;
                    case "FinancialStatements_NumberOfShareHolders_TwelveMonths":
                        return 36319;
                }
                return null;
            }
            public void Initialize(IDataProvider dataProvider, bool liveMode)
            {
            }
        }
    }
}

