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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class RangeConsolidatorTests
    {
        [Test]
        public void RangeConsolidatorReturnsExpectedValues()
        {
            using var consolidator = CreateConsolidator(100);
            var testValues = new List<decimal>() { 90m, 94.5m, 94m, 89.5m, 89m, 90.5m, 90m, 91.5m, 90m, 90.5m, 92.5m };
            var returnedBars = UpdateConsolidator(consolidator, testValues, "IBM");

            var expectedValues = GetRangeConsolidatorExpectedValues();
            RangeBar lastRangeBar = null;
            for (int index = 0; index < returnedBars.Count; index++)
            {
                var open = expectedValues[index][0];
                var low = expectedValues[index][1];
                var high = expectedValues[index][2];
                var close = expectedValues[index][3];
                var volume = expectedValues[index][4];

                // Check RangeBar's values
                Assert.AreEqual(open, returnedBars[index].Open);
                Assert.AreEqual(low, returnedBars[index].Low);
                Assert.AreEqual(high, returnedBars[index].High);
                Assert.AreEqual(close, returnedBars[index].Close);
                Assert.AreEqual(volume, returnedBars[index].Volume);

                // Check the size of each RangeBar
                Assert.AreEqual(1, Math.Round(returnedBars[index].High - returnedBars[index].Low, 2));

                // Check the Open value of the current bar is outside last bar Low-High interval
                if (lastRangeBar != null)
                {
                    Assert.IsTrue(returnedBars[index].Open < lastRangeBar.Low || returnedBars[index].Open > lastRangeBar.High);
                }

                lastRangeBar = returnedBars[index];
            }
        }

        [TestCaseSource(nameof(PriceGapBehaviorIsTheExpectedOneTestCases))]
        public virtual void PriceGapBehaviorIsTheExpectedOne(Symbol symbol, double minimumPriceVariation, double range)
        {
            using var consolidator = CreateConsolidator((int)range);
            var testValues = new List<decimal>() { 90m, 94.5m, 94m, 89.5m, 89m, 90.5m, 90m, 91.5m, 90m, 90.5m, 92.5m };
            var returnedBars = UpdateConsolidator(consolidator, testValues, symbol);
            RangeBar lastRangeBar = null;
            for (int index = 0; index < returnedBars.Count; index++)
            {
                // Check the gap between each bar is of the size of the minimum price variation
                if (lastRangeBar != null)
                {
                    Assert.IsTrue(returnedBars[index].Open == (lastRangeBar.High + (decimal)minimumPriceVariation) || returnedBars[index].Open == (lastRangeBar.Low - (decimal)minimumPriceVariation));
                }
                lastRangeBar = returnedBars[index];
            }
        }

        [TestCaseSource(nameof(ConsolidatorCreatesExpectedBarsTestCases))]
        public virtual void ConsolidatorCreatesExpectedBarsInDifferentScenarios(List<decimal> testValues, RangeBar[] expectedBars)
        {
            using var consolidator = CreateConsolidator(100);
            var returnedBars = UpdateConsolidator(consolidator, testValues, Symbols.IBM);

            Assert.IsNotEmpty(returnedBars);
            for (int index = 0; index < returnedBars.Count; index++)
            {
                Assert.AreEqual(expectedBars[index].Open, returnedBars[index].Open);
                Assert.AreEqual(expectedBars[index].Low, returnedBars[index].Low);
                Assert.AreEqual(expectedBars[index].High, returnedBars[index].High);
                Assert.AreEqual(expectedBars[index].Close, returnedBars[index].Close);
                Assert.AreEqual(expectedBars[index].Volume, returnedBars[index].Volume);
                Assert.AreEqual(expectedBars[index].EndTime, returnedBars[index].EndTime);
            }
        }

        [TestCase(new double[] { 94, 94.1, 94.2, 94.3, 94.4, 94.5, 94.6, 94.7, 94.8, 94.9, 95, 95.1 }, new double[] { 94, 95, 94, 95, 110 })]
        [TestCase(new double[] { 94, 93.9, 93.8, 93.7, 93.6, 93.5, 93.4, 93.3, 93.2, 93.1, 93, 92.9 }, new double[] { 94, 94, 93, 93, 110 })]
        [TestCase(new double[] { 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 94, 95.1 }, new double[] { 94, 95, 94, 95, 160 })]
        [TestCase(new double[] { 94, 93.9, 94.1, 93.8, 94.2, 93.7, 94.3, 93.6, 94.4, 93.5, 94.5, 93.4 }, new double[] { 94, 94.5, 93.5, 93.5, 110 })]
        public void ConsolidatorUpdatesTheVolumeOfTheBarsAsExpected(double[] testValues, double[] expectedBar)
        {
            using var consolidator = CreateConsolidator(100);
            var returnedBars = UpdateConsolidator(consolidator, new List<decimal>(testValues.Select(x => (decimal)x)), Symbols.IBM);

            Assert.AreEqual(1, returnedBars.Count);
            Assert.AreEqual(expectedBar[0], returnedBars[0].Open);
            Assert.AreEqual(expectedBar[1], returnedBars[0].High);
            Assert.AreEqual(expectedBar[2], returnedBars[0].Low);
            Assert.AreEqual(expectedBar[3], returnedBars[0].Close);
            Assert.AreEqual(expectedBar[4], returnedBars[0].Volume);
        }

        protected virtual RangeConsolidator CreateConsolidator(int range)
        {
            return new RangeConsolidator(range, x => x.Value, x => 10m);
        }

        private List<RangeBar> UpdateConsolidator(RangeConsolidator rangeConsolidator, List<decimal> testValues, Symbol symbol)
        {
            var time = new DateTime(2016, 1, 1);
            using var consolidator = rangeConsolidator;
            var returnedBars = new List<RangeBar>();

            consolidator.DataConsolidated += (sender, rangeBar) =>
            {
                returnedBars.Add(rangeBar);
            };

            for (int i = 0; i < testValues.Count; i++)
            {
                var data = new IndicatorDataPoint(symbol, time.AddDays(i), testValues[i]);
                consolidator.Update(data);
            }

            return returnedBars;
        }

        private static object[] ConsolidatorCreatesExpectedBarsTestCases = new object[]
        {
            new object[] { new List<decimal>(){ 90m, 94.5m }, new RangeBar[] {
                new RangeBar{ Open = 90m, Low = 90m, High = 91m, Close = 91m, Volume = 10m,  EndTime = new DateTime(2016, 1, 2)},
                new RangeBar{ Open = 91.01m, Low = 91.01m, High = 92.01m, Close = 92.01m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar{ Open = 92.02m, Low = 92.02m, High = 93.02m, Close = 93.02m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar{ Open = 93.03m, Low = 93.03m, High = 94.03m, Close = 94.03m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
            }},
            new object[] { new List<decimal>(){ 94m, 89.5m }, new RangeBar[] {
                new RangeBar { Open = 94m, Low = 93m, High = 94m, Close = 93m, Volume = 10m, EndTime = new DateTime(2016, 1, 2)},
                new RangeBar { Open = 92.99m, Low = 91.99m, High = 92.99m, Close = 91.99m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 91.98m, Low = 90.98m, High = 91.98m, Close = 90.98m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 90.97m, Low = 89.97m, High = 90.97m, Close = 89.97m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) }
            }},
            new object[] { new List<decimal>{ 90m, 94.5m, 89.5m }, new RangeBar[] {
                new RangeBar { Open = 90m, Low = 90m, High = 91m, Close = 91m, Volume = 10m , EndTime = new DateTime(2016, 1, 2)},
                new RangeBar { Open = 91.01m, Low = 91.01m, High = 92.01m, Close = 92.01m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 92.02m, Low = 92.02m, High = 93.02m, Close = 93.02m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 93.03m, Low = 93.03m, High = 94.03m, Close = 94.03m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 94.04m, Low = 93.50m, High = 94.50m, Close = 93.50m, Volume = 10m, EndTime = new DateTime(2016, 1, 3)},
                new RangeBar { Open = 93.49m, Low = 92.49m, High = 93.49m, Close = 92.49m, Volume = 0m, EndTime = new DateTime(2016, 1, 3) },
                new RangeBar { Open = 92.48m, Low = 91.48m, High = 92.48m, Close = 91.48m, Volume = 0m, EndTime = new DateTime(2016, 1, 3) },
                new RangeBar { Open = 91.47m, Low = 90.47m, High = 91.47m, Close = 90.47m, Volume = 0m, EndTime = new DateTime(2016, 1, 3) }
            }},
            new object[] { new List<decimal>{ 94.5m, 89.5m, 94.5m }, new RangeBar[] {
                new RangeBar { Open = 95m, Low = 94m, High = 95m, Close = 94m, Volume = 10m, EndTime = new DateTime(2016, 1, 2)},
                new RangeBar { Open = 93.99m, Low = 92.99m, High = 93.99m, Close = 92.99m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 92.98m, Low = 91.98m, High = 92.98m, Close = 91.98m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 91.97m, Low = 90.97m, High = 91.97m, Close = 90.97m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 90.96m, Low = 89.96m, High = 90.96m, Close = 89.96m, Volume = 0m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 89.95m, Low = 89.50m, High = 90.50m, Close = 90.50m, Volume = 10m, EndTime = new DateTime(2016, 1, 3)},
                new RangeBar { Open = 90.51m, Low = 90.51m, High = 91.51m, Close = 91.51m, Volume = 0m, EndTime = new DateTime(2016, 1, 3) },
                new RangeBar { Open = 91.52m, Low = 91.52m, High = 92.52m, Close = 92.52m, Volume = 0m, EndTime = new DateTime(2016, 1, 3) },
                new RangeBar { Open = 92.53m, Low = 92.53m, High = 93.53m, Close = 93.53m, Volume = 0m, EndTime = new DateTime(2016, 1, 3) },
            }},
            new object[] {new List<decimal> { 94m, 93.9m, 94.1m, 93.8m, 94.2m, 93.7m, 94.3m, 93.6m, 94.4m, 93.5m, 94.5m, 93.4m },
            new RangeBar[]{ new RangeBar { Open = 94m, High = 94.5m, Low = 93.5m, Close = 93.5m, Volume = 110, EndTime = new DateTime(2016, 1, 12) } }},
            new object[] {new List<decimal> { 94m, 94m, 94m, 94m, 94m, 95.1m },
            new RangeBar[]{ new RangeBar { Open = 94m, High = 95m, Low = 94m, Close = 95m, Volume = 50, EndTime = new DateTime(2016, 1, 6) } }}
        };

        protected static object[] PriceGapBehaviorIsTheExpectedOneTestCases = new object[]
        {
            new object[] { Symbols.XAUUSD, 0.001, 1000},
            new object[] { Symbols.XAGUSD, 0.00001, 100000},
            new object[] { Symbols.DE30EUR, 0.1, 10},
            new object[] { Symbols.XAUJPY, 1, 1}
        };

        protected virtual decimal[][] GetRangeConsolidatorExpectedValues()
        {
            return new decimal[][] {
                    new decimal[]{ 90m, 90m, 91m, 91m, 10m },
                    new decimal[]{ 91.01m, 91.01m, 92.01m, 92.01m, 0m },
                    new decimal[]{ 92.02m, 92.02m, 93.02m, 93.02m, 0m },
                    new decimal[]{ 93.03m, 93.03m, 94.03m, 94.03m, 0m },
                    new decimal[]{ 94.04m, 93.5m, 94.5m, 93.5m, 20m},
                    new decimal[]{ 93.49m, 92.49m, 93.49m, 92.49m, 0m},
                    new decimal[]{ 92.48m, 91.48m, 92.48m, 91.48m, 0m},
                    new decimal[]{ 91.47m, 90.47m, 91.47m, 90.47m, 0m},
                    new decimal[]{ 90.46m, 89.46m, 90.46m, 89.46m, 10m},
                    new decimal[]{ 89.45m, 89m, 90m, 90m, 10m},
                    new decimal[]{ 90.01m, 90m, 91m, 91m, 20m},
                    new decimal[]{ 91.01m, 90.5m, 91.5m, 90.5m, 10m},
                    new decimal[]{ 90.49m, 90m, 91m, 91m, 20m},
                    new decimal[]{ 91.01m, 91.01m, 92.01m, 92.01m, 0m }
                };
        }
    }
}
