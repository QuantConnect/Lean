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
using QuantConnect.Data.Market;
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

        [SetUp]
        public void Setup()
        {
            SymbolCache.Clear();
            MarketHoursDatabase.Reset();

            // Using a date that we have data for in the repo
            _startDate = new DateTime(2014, 3, 31);
            _endDate = new DateTime(2014, 3, 31);

            using (Py.GIL())
            {
                _module = Py.Import("Test_QuantBookHistory");
            }
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
            var qb = new QuantBook();
            var data = qb.GetFundamental(input[0], input[1], _startDate, _endDate);

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
            var qb = new QuantBook();
            var data = qb.GetFundamental(input[0], input[1], input[2], input[3]);
            Assert.IsEmpty(data);
        }

        // Different requests and their expected values
        private static readonly object[] DataTestCases =
        {
            new object[] {new List<string> {"AAPL"}, "ValuationRatios.PERatio", 13.272502m},
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

