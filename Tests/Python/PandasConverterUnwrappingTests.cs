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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Python;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Python
{
    [TestFixture]
    public partial class PandasConverterTests
    {
        private class CustomBar
        {
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
        }

        private class CustomTradeBar : BaseData
        {
            public CustomBar Prices { get; set; }

            public decimal Volume { get; set; }

            public CustomTradeBar(Symbol symbol, decimal open, decimal high, decimal low, decimal close, decimal volume)
            {
                Symbol = symbol;
                Prices = new CustomBar
                {
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close
                };
                Volume = volume;
            }
        }

        private class CustomQuoteBar : CustomBar, ISymbolProvider
        {
            public Symbol Symbol { get; set; }

            public CustomBar Bid { get; set; }

            public CustomBar Ask { get; set; }
        }

        [Test]
        public void UnpacksNestedClassesIntoDataFrameColumns()
        {
            var converter = new PandasConverter();
            var data = new List<CustomTradeBar>
            {
                new CustomTradeBar(Symbols.IBM, 101m, 102m, 100m, 101m, 10m),
                new CustomTradeBar(Symbols.IBM, 102m, 103m, 101m, 101m, 9m),
                new CustomTradeBar(Symbols.IBM, 99m, 100m, 98m, 99m, 10m),
            };

            dynamic dataFrame = converter.GetDataFrame(data);

            Console.WriteLine((string)dataFrame.to_string());

            var expectedColumnNames = new List<string>() { "open", "high", "low", "close", "volume" };

            AssertDataFrameColumns(dataFrame, data.Count, expectedColumnNames);
        }

        public class TestInnerData1
        {
            public decimal DecimalValue1 { get; set; }
            public string StringValue1 { get; set; }

        }

        [PandasColumn]
        public class TestInnerData2
        {
            public decimal DecimalValue2 { get; set; }
            public string StringValue2 { get; set; }
        }

        public class TestData1 : BaseData
        {
            public TestInnerData1 TestInnerData1 { get; set; }
            public TestInnerData2 TestInnerData2 { get; set; }

            [PandasColumn]
            public TestInnerData1 TestInnerData3 { get; set; }

            public TestData1(Symbol symbol, decimal decimalValue1, string stringValue1, decimal decimalValue2, string stringValue2,
                decimal decimalValue3, string stringValue3)
            {
                Symbol = symbol;
                TestInnerData1 = new TestInnerData1
                {
                    DecimalValue1 = decimalValue1,
                    StringValue1 = stringValue1,
                };
                TestInnerData2 = new TestInnerData2
                {
                    DecimalValue2 = decimalValue2,
                    StringValue2 = stringValue2,
                };
                TestInnerData3 = new TestInnerData1
                {
                    DecimalValue1 = decimalValue3,
                    StringValue1 = stringValue3,
                };
            }
        }

        [Test]
        public void DoesNotUnpackMarkedPropertiesAndClasses()
        {
            var converter = new PandasConverter();
            var data = new List<TestData1>
            {
                new TestData1(Symbols.IBM, 1m, "Test 1.1", 2m, "Test 1.2", 3m, "Test 1.3"),
                new TestData1(Symbols.IBM, 10m, "Test 2.1", 20m, "Test 2.2", 30m, "Test 2.3"),
                new TestData1(Symbols.IBM, 100m, "Test 3.1", 200m, "Test 3.2", 300m, "Test 3.3"),
            };

            dynamic dataFrame = converter.GetDataFrame(data);

            Console.WriteLine((string)dataFrame.to_string());

            var expectedColumnNames = new List<string>() { "decimalvalue1", "stringvalue1", "testinnerdata2", "testinnerdata3" };

            AssertDataFrameColumns(dataFrame, data.Count, expectedColumnNames);

            using var _ = Py.GIL();

            foreach (var value in dataFrame["testinnerdata2"])
            {
                Assert.DoesNotThrow(() => value.As<TestInnerData2>());
            }

            foreach (var value in dataFrame["testinnerdata3"])
            {
                Assert.DoesNotThrow(() => value.As<TestInnerData1>());
            }
        }

        private static void AssertDataFrameColumns(dynamic dataFrame, int dataCount, List<string> expectedColumnNames)
        {
            using var _ = Py.GIL();

            Assert.AreEqual(dataCount, dataFrame.shape[0].As<int>());
            Assert.AreEqual(expectedColumnNames.Count, dataFrame.shape[1].As<int>());

            var columnNames = new List<string>();
            foreach (var pandasColumn in dataFrame.columns.to_list())
            {
                columnNames.Add(pandasColumn.__str__().As<string>());
            }
            CollectionAssert.AreEquivalent(expectedColumnNames, columnNames);
        }

        private class TestInnerData3
        {
            public decimal TestValue1 { get; set; }

            public decimal TestValue2 { get; set; }

            [PandasIgnore]
            public decimal IgnoredValue { get; set; }
        }

        private class TestData2 : BaseData
        {
            public TestInnerData3 InnerData { get; set; }

            public string MainValue { get; set; }

            public TestData2(Symbol symbol, string mainValue, decimal testValue1, decimal testValue2, decimal ignoredValue)
            {
                Symbol = symbol;
                MainValue = mainValue;
                InnerData = new TestInnerData3()
                {
                    TestValue1 = testValue1,
                    TestValue2 = testValue2,
                    IgnoredValue = ignoredValue
                };
            }
        }

        [Test]
        public void OmitsPropertiesMarkedToBeIgnored()
        {
            var converter = new PandasConverter();
            var data = new List<TestData2>
            {
                new TestData2(Symbols.IBM, "Main value 1", 10m, 200m, 5m),
                new TestData2(Symbols.IBM, "Main value 2", 20m, 300m, 10m),
                new TestData2(Symbols.IBM, "Main value 3", 30m, 400m, 15m),
            };

            dynamic dataFrame = converter.GetDataFrame(data);

            Console.WriteLine((string)dataFrame.to_string());

            var expectedColumnNames = new List<string>() { "mainvalue", "testvalue1", "testvalue2" };

            AssertDataFrameColumns(dataFrame, data.Count, expectedColumnNames);
        }
    }
}
