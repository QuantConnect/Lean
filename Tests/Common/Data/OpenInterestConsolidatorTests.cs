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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class OpenInterestConsolidatorTests : BaseConsolidatorTests
    {
        [TestCaseSource(nameof(HourAndDailyTestValues))]
        public void HourAndDailyConsolidationKeepsTimeOfDay(TimeSpan period, List<(OpenInterest, bool)> data)
        {
            using var consolidator = new OpenInterestConsolidator(period);

            var consolidatedOpenInterest = (OpenInterest)null;
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                Log.Debug($"{consolidated.EndTime} - {consolidated}");
                consolidatedOpenInterest = consolidated;
            };

            var prevData = (OpenInterest)null;
            foreach (var (openInterest, shouldConsolidate) in data)
            {
                consolidator.Update(openInterest);

                if (shouldConsolidate)
                {
                    Assert.IsNotNull(consolidatedOpenInterest);
                    Assert.AreEqual(prevData.Symbol, consolidatedOpenInterest.Symbol);
                    Assert.AreEqual(prevData.Value, consolidatedOpenInterest.Value);
                    Assert.AreEqual(prevData.EndTime, consolidatedOpenInterest.EndTime);
                    consolidatedOpenInterest = null;
                }
                else
                {
                    Assert.IsNull(consolidatedOpenInterest);
                }

                prevData = openInterest;
            }
        }

        protected override IDataConsolidator CreateConsolidator()
        {
            return new OpenInterestConsolidator(TimeSpan.FromDays(1));
        }

        protected override IEnumerable<IBaseData> GetTestValues()
        {
            var time = new DateTime(2015, 04, 13, 8, 31, 0);
            return new List<OpenInterest>()
            {
                new OpenInterest(){ Time = time, Symbol = Symbols.SPY, Value = 10 },
                new OpenInterest(){ Time = time.AddMinutes(1), Symbol = Symbols.SPY, Value = 12 },
                new OpenInterest(){ Time = time.AddMinutes(2), Symbol = Symbols.SPY, Value = 10 },
                new OpenInterest(){ Time = time.AddMinutes(3), Symbol = Symbols.SPY, Value = 5 },
                new OpenInterest(){ Time = time.AddMinutes(4), Symbol = Symbols.SPY, Value = 15 },
                new OpenInterest(){ Time = time.AddMinutes(5), Symbol = Symbols.SPY, Value = 20 },
                new OpenInterest(){ Time = time.AddMinutes(6), Symbol = Symbols.SPY, Value = 18 },
                new OpenInterest(){ Time = time.AddMinutes(7), Symbol = Symbols.SPY, Value = 12 },
                new OpenInterest(){ Time = time.AddMinutes(8), Symbol = Symbols.SPY, Value = 25 },
                new OpenInterest(){ Time = time.AddMinutes(9), Symbol = Symbols.SPY, Value = 30 },
                new OpenInterest(){ Time = time.AddMinutes(10), Symbol = Symbols.SPY, Value = 26 },
            };
        }

        private static IEnumerable<TestCaseData> HourAndDailyTestValues()
        {
            var symbol = Symbols.SPY_C_192_Feb19_2016;
            var time = new DateTime(2015, 04, 13, 6, 30, 0);
            var period = Time.OneDay;

            yield return new TestCaseData(
                period,
                new List<(OpenInterest, bool)>()
                {
                    (new OpenInterest(time, symbol, 10), false),
                    (new OpenInterest(time.AddDays(1), symbol, 11), true),
                    (new OpenInterest(time.AddDays(2), symbol, 12), true),
                    (new OpenInterest(time.AddDays(3), symbol, 13), true),
                    (new OpenInterest(time.AddDays(4), symbol, 14), true),
                    (new OpenInterest(time.AddDays(5), symbol, 15), true),
                });

            yield return new TestCaseData(
                period,
                new List<(OpenInterest, bool)>()
                {
                    (new OpenInterest(time, symbol, 10), false),
                    (new OpenInterest(time.AddDays(1), symbol, 11), true),
                    // Same date, should not consolidate
                    (new OpenInterest(time.AddDays(1).AddMinutes(1), symbol, 12), false),
                    // Same date, should not consolidate
                    (new OpenInterest(time.AddDays(1).AddMinutes(2), symbol, 13), false),
                    // Same date, should not consolidate
                    (new OpenInterest(time.AddDays(1).AddMinutes(3), symbol, 14), false),
                    // Not the full period passed but different date, should consolidate
                    (new OpenInterest(time.AddDays(2).AddHours(-1), symbol, 15), true),
                    (new OpenInterest(time.AddDays(3).AddHours(-2), symbol, 16), true),
                    (new OpenInterest(time.AddDays(4).AddHours(-3), symbol, 17), true),
                    (new OpenInterest(time.AddDays(5).AddHours(-4), symbol, 18), true),
                });

            period = Time.OneHour;

            yield return new TestCaseData(
                period,
                new List<(OpenInterest, bool)>()
                {
                    (new OpenInterest(time, symbol, 10), false),
                    (new OpenInterest(time.AddHours(1), symbol, 11), true),
                    (new OpenInterest(time.AddHours(2), symbol, 12), true),
                    (new OpenInterest(time.AddHours(3), symbol, 13), true),
                    (new OpenInterest(time.AddHours(4), symbol, 14), true),
                    (new OpenInterest(time.AddHours(5), symbol, 15), true),
                });

            yield return new TestCaseData(
                period,
                new List<(OpenInterest, bool)>()
                {
                    (new OpenInterest(time.AddHours(0.5).AddMinutes(10), symbol, 10), false),
                    (new OpenInterest(time.AddHours(2.5).AddMinutes(20), symbol, 11), true),
                    (new OpenInterest(time.AddHours(4.5).AddMinutes(30), symbol, 12), true),
                    (new OpenInterest(time.AddHours(6.5).AddMinutes(40), symbol, 13), true),
                    (new OpenInterest(time.AddHours(8.5), symbol, 14), true),
                    (new OpenInterest(time.AddHours(10.5).AddMinutes(50), symbol, 15), true),
                });

            yield return new TestCaseData(
                period,
                new List<(OpenInterest, bool)>()
                {
                    (new OpenInterest(time, symbol, 10), false),
                    (new OpenInterest(time.AddHours(1), symbol, 11), true),
                    // Same date, should not consolidate
                    (new OpenInterest(time.AddHours(1).AddMinutes(5), symbol, 12), false),
                    // Same date, should not consolidate
                    (new OpenInterest(time.AddHours(1).AddMinutes(10), symbol, 13), false),
                    // Same date, should not consolidate
                    (new OpenInterest(time.AddHours(1).AddMinutes(15), symbol, 14), false),
                    // Not the full period passed but different date, should consolidate
                    (new OpenInterest(time.AddHours(2).AddMinutes(-5), symbol, 15), true),
                    (new OpenInterest(time.AddHours(3).AddMinutes(-10), symbol, 16), true),
                    (new OpenInterest(time.AddHours(4).AddMinutes(-15), symbol, 17), true),
                    (new OpenInterest(time.AddHours(5).AddMinutes(-20), symbol, 18), true),
                });
        }
    }
}
