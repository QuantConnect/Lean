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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Python;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class RollingWindowTests
    {
        [Test]
        public void NewWindowIsEmpty()
        {
            var window = new RollingWindow<int>(1);
            Assert.AreEqual(1, window.Size);
            Assert.AreEqual(0, window.Count);
            Assert.AreEqual(0, window.Samples);
            Assert.IsFalse(window.IsReady);
        }

        [Test]
        public void AddsData()
        {
            var window = new RollingWindow<int>(2);
            Assert.AreEqual(0, window.Count);
            Assert.AreEqual(0, window.Samples);
            Assert.AreEqual(2, window.Size);
            Assert.IsFalse(window.IsReady);

            window.Add(1);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(1, window.Samples);
            Assert.AreEqual(2, window.Size);
            Assert.IsFalse(window.IsReady);

            // add one more and the window is ready
            window.Add(2);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(2, window.Samples);
            Assert.AreEqual(2, window.Size);
            Assert.IsTrue(window.IsReady);
        }

        [Test]
        public void OldDataFallsOffBackOfWindow()
        {
            var window = new RollingWindow<int>(1);
            Assert.IsFalse(window.IsReady);

            // add one and the window is ready, but MostRecentlyRemoved throws

            window.Add(0);
            Assert.Throws<InvalidOperationException>(() => { var x = window.MostRecentlyRemoved; });
            Assert.AreEqual(1, window.Count);
            Assert.IsTrue(window.IsReady);

            // add another one and MostRecentlyRemoved is available

            window.Add(1);
            Assert.AreEqual(0, window.MostRecentlyRemoved);
            Assert.AreEqual(1, window.Count);
            Assert.IsTrue(window.IsReady);
        }

        [Test]
        public void IndexingBasedOnReverseInsertedOrder()
        {
            var window = new RollingWindow<int>(3);
            Assert.AreEqual(3, window.Size);

            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);

            window.Add(1);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(0, window[1]);
            Assert.AreEqual(1, window[0]);

            window.Add(2);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(0, window[2]);
            Assert.AreEqual(1, window[1]);
            Assert.AreEqual(2, window[0]);

            window.Add(3);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(1, window[2]);
            Assert.AreEqual(2, window[1]);
            Assert.AreEqual(3, window[0]);
        }

        [Test]
        public void EnumeratesAsExpected()
        {
            var window = new RollingWindow<int>(3) { 0, 1, 2 };
            var inOrder = window.ToList();
            Assert.AreEqual(2, inOrder[0]);
            Assert.AreEqual(1, inOrder[1]);
            Assert.AreEqual(0, inOrder[2]);

            window.Add(3);
            var inOrder2 = window.ToList();
            Assert.AreEqual(3, inOrder2[0]);
            Assert.AreEqual(2, inOrder2[1]);
            Assert.AreEqual(1, inOrder2[2]);
        }

        [Test]
        public void ResetsProperly()
        {
            var window = new RollingWindow<int>(3) { 0, 1, 2 };
            window.Reset();
            Assert.AreEqual(0, window.Samples);
        }

        [Test]
        public void RetrievesNonZeroIndexProperlyAfterReset()
        {
            var window = new RollingWindow<int>(3);
            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);

            window.Add(1);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(0, window[1]);
            Assert.AreEqual(1, window[0]);

            window.Add(2);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(0, window[2]);
            Assert.AreEqual(1, window[1]);
            Assert.AreEqual(2, window[0]);

            window.Add(3);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(1, window[2]);
            Assert.AreEqual(2, window[1]);
            Assert.AreEqual(3, window[0]);

            window.Reset();
            window.Add(0);
            Assert.AreEqual(1, window.Count);
            Assert.AreEqual(0, window[0]);

            window.Add(1);
            Assert.AreEqual(2, window.Count);
            Assert.AreEqual(0, window[1]);
            Assert.AreEqual(1, window[0]);

            window.Add(2);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(0, window[2]);
            Assert.AreEqual(1, window[1]);
            Assert.AreEqual(2, window[0]);

            window.Add(3);
            Assert.AreEqual(3, window.Count);
            Assert.AreEqual(1, window[2]);
            Assert.AreEqual(2, window[1]);
            Assert.AreEqual(3, window[0]);
        }

        [Test]
        public void DoesNotThrowWhenIndexIsNegative()
        {
            var window = new RollingWindow<int>(1);
            Assert.IsFalse(window.IsReady);

            Assert.DoesNotThrow(() => { var x = window[-1]; });
        }

        [Test]
        public void WindowCanBeIndexedOutsideCount()
        {
            const int windowSize = 10;
            var window = new RollingWindow<int>(windowSize);

            // Add some data to the window
            const int count = windowSize / 2;
            for (var i = 0; i < count; i++)
            {
                window.Add(i);
            }
            Assert.AreEqual(count, window.Count);

            // Index the indicator outside the current count but within its size
            for (var i = window.Count; i < window.Size; i++)
            {
                Assert.AreEqual(default(int), window[i]);
                Assert.AreEqual(windowSize, window.Size);
            }
        }

        [Test]
        public void WindowCanBeIndexedOutsideCountUsingSetter()
        {
            const int windowSize = 20;
            const int initialElementsCount = 5;
            const int indexingStart = 10;
            var window = new RollingWindow<int>(windowSize);

            // Add some data to the window
            for (var i = 0; i < initialElementsCount; i++)
            {
                window.Add(i);
            }
            Assert.AreEqual(initialElementsCount, window.Count);

            // Index the indicator outside the current count but within its size
            for (var i = indexingStart; i < window.Size; i++)
            {
                window[i] = i;
                Assert.AreEqual(i, window[i]);
                Assert.AreEqual(windowSize, window.Size);
            }

            // The middle indexes that were not touched should have the default value
            for (var i = initialElementsCount; i < indexingStart; i++)
            {
                Assert.AreEqual(default(int), window[i]);
            }
        }

        [Test]
        public void WindowCanBeIndexedOutsideSizeAndGetsResized()
        {
            const int windowSize = 10;
            var window = new RollingWindow<int>(windowSize);

            // Index the indicator outside the current size
            for (var i = windowSize; i < windowSize + 10; i++)
            {
                Assert.AreEqual(default(int), window[i]);
                Assert.AreEqual(i + 1, window.Size);
            }

            // Explicitly resize the window
            var oldSize = window.Size;
            window.Size = window.Size + 10;
            for (var i = oldSize; i < window.Size; i++)
            {
                Assert.AreEqual(default(int), window[i]);
            }
        }

        [Test]
        public void WindowCanBeIndexedOutsideSizeUsingSetterAndGetsResized()
        {
            const int windowSize = 10;
            var window = new RollingWindow<int>(windowSize);

            // Index the indicator outside the current size
            for (var i = windowSize; i < windowSize + 10; i++)
            {
                window[i] = i;
                Assert.AreEqual(i, window[i]);
                Assert.AreEqual(i + 1, window.Size);
            }
        }

        [Test]
        public void NewElementsHaveDefaultValuesWhenResizingUp()
        {
            var window = new RollingWindow<int>(10);

            // Explicitly resize the window
            var oldSize = window.Size;
            window.Size = window.Size * 2;
            for (var i = oldSize; i < window.Size; i++)
            {
                Assert.AreEqual(default(int), window[i]);
            }
        }

        [Test]
        public void HistoryWindowResizingUpKeepsCurrentValues()
        {
            const int windowSize = 10;
            var window = new RollingWindow<int>(windowSize);

            // Fill the window up
            var values = new List<int>(window.Size);
            for (var i = 0; i < window.Size; i++)
            {
                window.Add(i);
                values.Insert(0, i);
            }

            // Resize up
            window.Size = window.Size * 2;
            Assert.AreEqual(values.Count, window.Count);
            CollectionAssert.AreEqual(values, window.Take(values.Count));
        }

        [Test]
        public void HistoryWindowResizingDownKeepsCurrentValuesWithinNewSizeBelowCurrentCount()
        {
            const int windowSize = 20;
            const int smallerSize = 10;

            var window = new RollingWindow<int>(windowSize);

            // Fill the window up
            var values = new List<int>(window.Size);
            for (var i = 0; i < window.Size; i++)
            {
                window.Add(i);
                values.Insert(0, i);
            }

            window.Size = smallerSize;
            Assert.AreEqual(smallerSize, window.Size);
            Assert.AreEqual(smallerSize, window.Count);
            CollectionAssert.AreEqual(values.Take(smallerSize), window);
        }

        [Test]
        public void HistoryWindowResizingDownKeepsCurrentValuesWithinNewSizeAboveCurrentCount()
        {
            const int windowSize = 20;
            const int dataCount = 5;
            const int smallerSize = 10;

            var window = new RollingWindow<int>(windowSize);

            // Add some data to the window
            var values = new List<int>(window.Size);
            for (var i = 0; i < dataCount; i++)
            {
                window.Add(i);
                values.Insert(0, i);
            }

            window.Size = smallerSize;
            Assert.AreEqual(smallerSize, window.Size);
            Assert.AreEqual(dataCount, window.Count);
            CollectionAssert.AreEqual(values.Take(smallerSize), window);
        }

        [Test]
        public void RollingWindowSupportsNegativeIndices()
        {
            var window = new RollingWindow<int>(5);

            // At initialization, all values should be 0, whether accessed with positive or negative indices.
            for (int i = 0; i < window.Size; i++)
            {
                Assert.AreEqual(0, window[0]);
                Assert.AreEqual(0, window[~i]);
            }

            // Add two elements and test negative indexing before window is full
            window.Add(7);
            window.Add(-2);
            Assert.AreEqual(0, window[-1]);
            Assert.AreEqual(2, window.Count);
            Assert.IsFalse(window.IsReady);

            // Fill the window to capacity
            window.Add(5);
            window.Add(1);
            window.Add(4);
            Assert.IsTrue(window.IsReady);

            // Test that -1 is the same as Count - 1
            Assert.AreEqual(window[window.Count - 1], window[-1]);

            // Verify full reverse access using negative indices
            for (int i = 0; i < window.Count; i++)
            {
                Assert.AreEqual(window[window.Count - 1 - i], window[~i]);
            }

            // Overwrite all values using negative indices
            for (int i = 1; i <= window.Count; i++)
            {
                window[-i] = i;
            }

            // Verify final state of the window after overwrite
            for (int i = 0; i < window.Count; i++)
            {
                Assert.AreEqual(window[window.Count - 1 - i], window[~i]);
            }
        }

        [Test]
        public void NegativeIndexThrowsWhenExceedingWindowSize()
        {
            var window = new RollingWindow<int>(3);

            // Fill window completely
            window.Add(10);
            window.Add(20);
            window.Add(30);

            // Valid negative indices
            Assert.AreEqual(10, window[-1]);
            Assert.AreEqual(20, window[-2]);
            Assert.AreEqual(30, window[-3]);

            // Invalid negative indices (exceeding window size)
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = window[-4]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = window[-5]; });
        }

        [Test]
        public void SetterThrowsForInvalidNegativeIndices()
        {
            var window = new RollingWindow<int>(2);
            window.Add(1);
            window.Add(2);

            // Valid sets
            window[-1] = 20;
            window[-2] = 10;

            // Verify valid changes were made
            Assert.AreEqual(10, window[0]);
            Assert.AreEqual(20, window[1]);

            // Invalid sets
            Assert.Throws<ArgumentOutOfRangeException>(() => window[-3] = 30);
            Assert.Throws<ArgumentOutOfRangeException>(() => window[-4] = 40);
        }

        [Test]
        public void MixedPositiveAndNegativeIndexBehavior()
        {
            var window = new RollingWindow<int>(4);

            // Fill window with test data
            for (int i = 1; i <= 4; i++)
            {
                window.Add(i * 10);
            }

            // Test all valid positions
            for (int i = 0; i < 4; i++)
            {
                var positiveIndexValue = window[i];
                var negativeIndexValue = window[-(4 - i)];
                Assert.AreEqual(positiveIndexValue, negativeIndexValue);
            }

            // Test invalid positions
            var testCases = new[] { -5, -10, int.MinValue };
            foreach (var index in testCases)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { var x = window[index]; });
            }
        }

        [TestCase("tuple", 3)]
        [TestCase("list", 6)]
        [TestCase("dict", 2)]
        [TestCase("float", 3.9)]
        [TestCase("trade_bar", 100)]
        [TestCase("quote_bar", 100)]
        [TestCase("custom_data_type", 123)]
        public void RollingWindowWorksWithAnyType(string type, decimal expectedValue)
        {
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("TestRollingWindow",
                    @"
from AlgorithmImports import *

class MyCustomDataType(PythonData):
    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live: bool) -> SubscriptionDataSource:
        fileName = LeanData.GenerateZipFileName(Symbols.SPY, date, Resolution.MINUTE, config.TickType)

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live: bool) -> BaseData:
        data = line.split(',')
        result = MyCustomDataType()

def rolling_window_with_tuple():
    rollingWindow = RollingWindow(5)
    rollingWindow.add((1, ""a""))
    rollingWindow.add((2, ""b""))
    rollingWindow.add((3, ""c""))
    return rollingWindow[0][0]

def rolling_window_with_list():
    rollingWindow = RollingWindow(5)
    rollingWindow.add([1, 2, 3])
    rollingWindow.add([5])
    rollingWindow.add([6, 7, 8])
    return rollingWindow[0][0]

def rolling_window_with_dict():
    rollingWindow = RollingWindow(5)
    rollingWindow.add({""key1"": 1, ""key2"": ""a""})
    rollingWindow.add({""key1"": 2, ""key2"": ""b""})
    return rollingWindow[0][""key1""]

def rolling_window_with_float():
    rollingWindow = RollingWindow(5)
    rollingWindow.add(1.5)
    rollingWindow.add(2.7)
    rollingWindow.add(3.9)
    return rollingWindow[0]

def rolling_window_with_trade_bar():
    rollingWindow = RollingWindow(5)
    bar1 = TradeBar()
    bar1.close = 100
    rollingWindow.add(bar1)
    return rollingWindow[0].close

def rolling_window_with_quote_bar():
    rollingWindow = RollingWindow(5)
    bar1 = QuoteBar()
    bar1.value = 100
    rollingWindow.add(bar1)
    return rollingWindow[0].value

def rolling_window_with_custom_data_type():
    rollingWindow = RollingWindow(5)
    customData = PythonData(MyCustomDataType())
    customData.test = 123
    rollingWindow.add(customData)
    return rollingWindow[0].test
");
                var methodName = "rolling_window_with_" + type;

                var test = testModule.GetAttr(methodName).Invoke();
                var value = test.As<decimal>();
                Assert.AreEqual(expectedValue, value);
            }
        }
    }
}
