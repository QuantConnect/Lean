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
        [Test]
        public void RangeConsolidatorReturnsExpectedValues()
        {
            var time = new DateTime(2016, 1, 1);
            var testValues = new List<decimal>() { 90m, 94.5m, 94m, 89.5m, 89m, 90.5m, 90m, 91.5m, 90m, 90.5m, 92.5m };

            var returnedBars = new List<RangeBar>();

            using var consolidator = CreateConsolidator();
            consolidator.DataConsolidated += (sender, rangeBar) =>
            {
                returnedBars.Add(rangeBar);
            };

            for (int i = 0; i < testValues.Count; i++)
            {
                var data = new IndicatorDataPoint(Symbols.IBM, time.AddSeconds(i), testValues[i]);
                consolidator.Update(data);
            }

            var expectedValues = GetRangeConsolidatorExpectedValues();
            for (int index = 0; index < returnedBars.Count; index++)
            {
                var open = expectedValues[index][0];
                var low = expectedValues[index][1];
                var high = expectedValues[index][2];
                var close = expectedValues[index][3];
                var volume = expectedValues[index][4];

                Assert.AreEqual(open, returnedBars[index].Open);
                Assert.AreEqual(low, returnedBars[index].Low);
                Assert.AreEqual(high, returnedBars[index].High);
                Assert.AreEqual(close, returnedBars[index].Close);
                Assert.AreEqual(volume, returnedBars[index].Volume);
            }
        }

        protected virtual RangeConsolidator CreateConsolidator()
        {
            return new RangeConsolidator(100m, x => x.Value, x => 10m);
        }

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
