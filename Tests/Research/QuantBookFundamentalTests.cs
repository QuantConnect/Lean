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
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Research;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantBookFundamentalTests
    {
        private dynamic _testModule;
        private DateTime _startDate;
        private DateTime _endDate;

        [SetUp]
        public void Setup()
        {
            // Using a date that we have data for in the repo
            _startDate = new DateTime(2014, 3, 31);
            _endDate = new DateTime(2014, 4, 1);

            using (Py.GIL())
            {
                _testModule = Py.Import("Test_QuantBookFundamental").GetAttr("FundamentalHistoryTest").Invoke();
            }
        }

        // All 4 of the accepted types of input for fundamental data request, with varying selectors
        private static readonly object[] FundamentalRequestTestCases =
        {
            new object[] {new List<string> {"AAPL", "GOOG"}, "ValuationRatios.PERatio"},
            new object[] {"IBM", "EarningReports.AccessionNumber"},
            new object[] {Symbol.Create("AAPL", SecurityType.Equity, Market.USA), "ValuationRatios"},
            new object[]
            {
                new List<Symbol>
                {
                    Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                    Symbol.Create("BAC", SecurityType.Equity, Market.USA)
                },
                "FinancialStatements"
            }
        };

        [TestCaseSource(nameof(FundamentalRequestTestCases))]
        public void PyFundamentalReturnTypes(dynamic input)
        {
            using (Py.GIL())
            {
                // Skip string test because it will fail;
                // Has to be tested inside of a Notebook to work as it should, due to PythonNet converting the string automatically
                // We implemented a bool variable in QuantBook's constructor (_isPythonNotebook), when a string is sent
                // to GetFundamental we check the bool and route it to the appropriate Python function if it is True.
                if (input[0].GetType() == typeof(string))
                {
                    return;
                }

                var data = _testModule.getFundamentals(input[0], input[1], _startDate, _endDate);
                Assert.IsTrue(data.GetType() == typeof(PyObject));
            }
        }

        [TestCaseSource(nameof(FundamentalRequestTestCases))]
        public void CSharpFundamentalReturnTypes(dynamic input)
        {
            var qb = new QuantBook();
            var data = qb.GetFundamental(input[0], input[1], _startDate, _endDate);
            Assert.IsTrue(data.GetType() == typeof(DataDictionary<IEnumerable<object>>));
        }

        // Different requests and their expected hashes
        private static readonly object[] PyDataTestCases =
        {
            new object[] {new List<string> {"AAPL"}, "ValuationRatios.PERatio", 295370642},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios", 292128215},
            new object[] {new List<Symbol> {Symbol.Create("AIG", SecurityType.Equity, Market.USA),}, "FinancialStatements", 354614656}
        };

        [TestCaseSource(nameof(PyDataTestCases))]
        public void PyFundamentalData(dynamic input)
        {
            using (Py.GIL())
            {
                var data = _testModule.getFundamentals(input[0], input[1], _startDate, _endDate);
                Assert.AreEqual(input[2], data.ToString().GetHashCode());
            }
        }

        // Different requests and their expected values
        private static readonly object[] CSharpDataTestCases =
        {
            new object[] {new List<string> {"AAPL"}, "ValuationRatios.PERatio", 13.272502m},
            new object[] {"BAC", "ValuationRatios.PERatio", 19.111111d},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios.BookValuePerShare", 22.5177},
            new object[] {new List<Symbol> {Symbol.Create("AIG", SecurityType.Equity, Market.USA)}, "FinancialStatements.NumberOfShareHolders", 36319}
        }; 

          [TestCaseSource(nameof(CSharpDataTestCases))]
        public void CSharpFundamentalData(dynamic input)
        {
            var qb = new QuantBook();
            var data = qb.GetFundamental(input[0], input[1], _startDate, _endDate);

            foreach (var collection in data.Values)
            {
                foreach (var selectedData in collection)
                {
                    Assert.AreEqual(input[2], selectedData.Value);
                    Assert.AreEqual(_startDate, selectedData.Time);
                }
            }
        }

        // Different requests that should all return null
        // Nonexistent data; No delta time; start date after end date
        private static readonly object[] NullRequestTestCases =
        {
            new object[] {Symbol.Create("AIG", SecurityType.Equity, Market.USA), "ValuationRatios.PERatio", new DateTime(1990, 4, 1),  new DateTime(1990, 4, 1)},
            new object[] {Symbol.Create("AAPL", SecurityType.Equity, Market.USA), "ValuationRatios.PERatio", new DateTime(2014, 3, 31), new DateTime(2014, 3, 31)},
            new object[] {Symbol.Create("IBM", SecurityType.Equity, Market.USA), "ValuationRatios.BookValuePerShare", new DateTime(2014, 4, 1), new DateTime(2014, 3, 31)},
        };

        [TestCaseSource(nameof(NullRequestTestCases))]
        public void PyReturnNullTest(dynamic input)
        {
            using (Py.GIL())
            {
                var data = _testModule.getFundamentals(input[0], input[1], input[2], input[3]);
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
    }
}

