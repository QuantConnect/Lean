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
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data 
{
    [TestFixture]
    public class RangeBarConsolidatorTests 
    {
        [Test]
        public void ConsolidatesRangeBars()
        {
            var list = new List<RangeBar>();

            var consolidator = new RangeBarConsolidator(2);
            consolidator.DataConsolidated += (sender, consolidated) => 
            {
                list.Add((RangeBar)consolidated);
            };

            var dt = DateTime.UtcNow;
            var testData = new[]
            {
                new Tick()
                {
                    Time = dt,
                    Value = 1
                },
                new Tick()
                {
                    Time = dt.AddMinutes(2),
                    Value = 3
                },
                new Tick()
                {
                    Time = dt.AddMinutes(3),
                    Value = 5
                },
                new Tick()
                {
                    Time = dt.AddMinutes(4),
                    Value = 6
                },
            };

            // Push first 3 data items
            for (var i = 0; i < 3; i++)
            {
                consolidator.Update(testData[i]);
            }
            
            Assert.AreEqual(1, list.Count);

            var bar = list[0];
            Assert.AreEqual(1, bar.Open);
            Assert.AreEqual(3, bar.Close);
            Assert.AreEqual(1, bar.Low);
            Assert.AreEqual(3, bar.High);

            // Push the 4th item
            consolidator.Update(testData[3]);
            
            Assert.AreEqual(2, list.Count);

            bar = list[1];
            Assert.AreEqual(3, bar.Open);
            Assert.AreEqual(5, bar.Close);
            Assert.AreEqual(3, bar.Low);
            Assert.AreEqual(5, bar.High);

            consolidator.Dispose();
        }

        [Test]
        public void ConsolidatesNewBarEveryTick()
        {
            var counter = 0;

            var consolidator = new RangeBarConsolidator(2);
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                counter++;
            };

            var dt = DateTime.UtcNow;
            var testData = new[]
            {
                new Tick()
                {
                    Time = dt,
                    Value = 1
                },
                new Tick()
                {
                    Time = dt.AddMinutes(1),
                    Value = 5
                },
                new Tick()
                {
                    Time = dt.AddMinutes(2),
                    Value = 9
                }
            };

            foreach (var tick in testData)
            {
                consolidator.Update(tick);
            }

            Assert.AreEqual(2, counter);
            consolidator.Dispose();
        }
    }
}
