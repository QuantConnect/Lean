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

using System;
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class PeriodCountConsolidatorTests
    {
        private static readonly object[] PeriodCases =
        {
            new [] { TimeSpan.FromDays(100), TimeSpan.FromDays(10) },
            new [] { TimeSpan.FromDays(30), TimeSpan.FromDays(1) },     //GH Issue #4915
            new [] { TimeSpan.FromDays(10), TimeSpan.FromDays(1) },
            new [] { TimeSpan.FromDays(1), TimeSpan.FromHours(1) },
            new [] { TimeSpan.FromHours(10), TimeSpan.FromHours(1) },
            new [] { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1) },
            new [] { TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10) },
            new [] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.1) },
        };

        [TestCaseSource(nameof(PeriodCases))]
        public void ExpectedConsolidatedTradeBarsInPeriodMode(TimeSpan barSpan, TimeSpan updateSpan)
        {
            TradeBar consolidated = null;
            var consolidator = new BaseDataConsolidator(barSpan);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.AreEqual(barSpan, bar.Period);              // The period matches our span
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            var dataTime = reference;

            var nextBarTime = reference + barSpan;
            var lastBarTime = reference;

            // First data point
            consolidator.Update(new Tick { Time = dataTime });
            Assert.IsNull(consolidated);

            for (var i = 0; i < 10; i++)
            {
                // Add data on the given interval until we expect a new bar
                while (dataTime < nextBarTime)
                {
                    dataTime = dataTime.Add(updateSpan);
                    consolidator.Update(new Tick { Time = dataTime });
                }

                // Our asserts
                Assert.IsNotNull(consolidated);                                 // We have a bar
                Assert.AreEqual(dataTime, consolidated.EndTime);    // New bar time should be dataTime
                Assert.AreEqual(barSpan, consolidated.EndTime - lastBarTime);      // The difference between the bars is the span

                nextBarTime = dataTime + barSpan;
                lastBarTime = consolidated.EndTime;
            }
        }

        [TestCaseSource(nameof(PeriodCases))]
        public void ExpectedConsolidatedQuoteBarsInPeriodMode(TimeSpan barSpan, TimeSpan updateSpan)
        {
            QuoteBar consolidated = null;
            var consolidator = new QuoteBarConsolidator(barSpan);
            consolidator.DataConsolidated += (sender, bar) =>
            {
                Assert.AreEqual(barSpan, bar.Period);                  // The period matches our span
                consolidated = bar;
            };

            var reference = new DateTime(2015, 04, 13);
            var dataTime = reference;

            var nextBarTime = reference + barSpan;
            var lastBarTime = reference;

            // First data point
            consolidator.Update(new QuoteBar { Time = dataTime });
            Assert.IsNull(consolidated);

            for (var i = 0; i < 10; i++)
            {
                // Add data on the given interval until we expect a new bar
                while (dataTime < nextBarTime)
                {
                    dataTime = dataTime.Add(updateSpan);
                    consolidator.Update(new QuoteBar { Time = dataTime });
                }

                // Our asserts
                Assert.IsNotNull(consolidated);                                 // We have a bar
                Assert.AreEqual(dataTime, consolidated.EndTime);    // New bar time should be dataTime
                Assert.AreEqual(barSpan, consolidated.EndTime - lastBarTime);      // The difference between the bars is the span

                nextBarTime = dataTime + barSpan;
                lastBarTime = consolidated.EndTime;
            }
        }
    }
}
