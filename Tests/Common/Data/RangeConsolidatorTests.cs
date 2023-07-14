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

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class RangeConsolidatorTests
    {
        [TestCaseSource(nameof(RangeBarConsolidatorReturnsExpectedValuesCases))]
        public void RangeConsolidatorReturnsExpectedValues(decimal[] inputValues, decimal[][] expectedValues)
        {
            var time = new DateTime(2016, 1, 1);
            var testValues = new List<decimal>(inputValues);

            var returnedBars = new List<RangeBar>();

            using var consolidator = new RangeConsolidator(1m);
            consolidator.DataConsolidated += (sender, rangeBar) =>
            {
                returnedBars.Add(rangeBar);
            };

            for (int i = 0; i < testValues.Count; i++)
            {
                var data = new IndicatorDataPoint(time.AddSeconds(i), testValues[i]);
                consolidator.Update(data);
            }

            for (int index = 0; index < returnedBars.Count; index++)
            {
                var open = expectedValues[index][0];
                var low = expectedValues[index][1];
                var high = expectedValues[index][2];
                var close = expectedValues[index][3];

                Assert.AreEqual(open, returnedBars[index].Open);
                Assert.AreEqual(low, returnedBars[index].Low);
                Assert.AreEqual(high, returnedBars[index].High);
                Assert.AreEqual(close, returnedBars[index].Close);
            }
        }

        public static object[] RangeBarConsolidatorReturnsExpectedValuesCases =
        {
            new object[] { new decimal[] { 8175.0m, 8175.5m, 8174.5m, 8174.0m, 8173.5m, 8173.9m, 8176m, 8176m, 8176m, 8176m, 8176.5m, 8170m },
                new decimal[][] { new decimal[]{ 8175, 8174.5m, 8175.5m, 8174.5m }, new decimal[]{ 8174m, 8173.5m, 8174.5m, 8174.5m }, new decimal[]{ 8176m, 8175.5m, 8176.5m, 8175.5m }} }
        };
    }
}
