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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class ExpiryTests
    {
        [Test]
        public void ExpiryEndOfWeekTests()
        {
            var endOfWeekList = new List<DateTime>();

            var current = new DateTime(2019, 1, 1);
            var end = new DateTime(2020, 1, 1);
            while (current < end)
            {
                var endOfWeek = Expiry.EndOfWeek(current);
                endOfWeekList.Add(endOfWeek);
                Assert.AreEqual(DayOfWeek.Monday, endOfWeek.DayOfWeek);
                Assert.Greater(endOfWeek, current);
                current = current.AddDays(1);
            }

            var actual = endOfWeekList.Distinct().Count();
            Assert.AreEqual(53, actual);
        }

        [Test]
        public void ExpiryEndOfMonthTests()
        {
            var endOfMonthList = new List<DateTime>();

            var current = new DateTime(2019, 1, 1);
            var end = new DateTime(2020, 1, 1);
            while (current < end)
            {
                var endOfMonth = Expiry.EndOfMonth(current);
                endOfMonthList.Add(endOfMonth);
                Assert.AreEqual(1, endOfMonth.Day);
                Assert.Greater(endOfMonth, current);
                current = current.AddDays(1);
            }

            var actual = endOfMonthList.Distinct().Count();
            Assert.AreEqual(12, actual);
        }

        [Test]
        public void ExpiryEndOfQuarterTests()
        {
            var endOfQuarterList = new List<DateTime>();

            var current = new DateTime(2019, 1, 1);
            var end = new DateTime(2020, 1, 1);
            while (current < end)
            {
                var endOfQuarter = Expiry.EndOfQuarter(current);
                endOfQuarterList.Add(endOfQuarter);
                Assert.AreEqual(1, endOfQuarter.Day);
                Assert.AreEqual(1, endOfQuarter.Month % 3);
                Assert.Greater(endOfQuarter, current);
                current = current.AddDays(1);
            }

            var actual = endOfQuarterList.Distinct().Count();
            Assert.AreEqual(4, actual);
        }

        [Test]
        public void ExpiryEndOfYearTests()
        {
            var endOfYearList = new List<DateTime>();

            var current = new DateTime(2019, 1, 1);
            var end = new DateTime(2020, 1, 1);
            while (current < end)
            {
                var endOfYear = Expiry.EndOfYear(current);
                endOfYearList.Add(endOfYear);
                Assert.AreEqual(1, endOfYear.Day);
                Assert.AreEqual(end.Year, endOfYear.Year);
                Assert.Greater(endOfYear, current);
                current = current.AddDays(1);
            }

            var actual = endOfYearList.Distinct().Count();
            Assert.AreEqual(1, actual);
        }
    }
}
