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
using QuantConnect.ToolBox.RandomDataGenerator;
using System;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomValueGeneratorTests
    {
        private const int Seed = 123456789;
        private RandomValueGenerator randomValueGenerator;

        [SetUp]
        public void Setup()
        {
            // initialize using a seed for deterministic tests
            randomValueGenerator = new RandomValueGenerator(Seed);
        }

        [Test]
        public void NextDateTime_CreatesDateTime_WithinSpecifiedMinMax()
        {
            var min = new DateTime(2000, 01, 01);
            var max = new DateTime(2001, 01, 01);
            var dateTime = randomValueGenerator.NextDate(min, max, dayOfWeek: null);

            Assert.LessOrEqual(min, dateTime);
            Assert.GreaterOrEqual(max, dateTime);
        }

        [Test]
        [TestCase(DayOfWeek.Sunday)]
        [TestCase(DayOfWeek.Monday)]
        [TestCase(DayOfWeek.Tuesday)]
        [TestCase(DayOfWeek.Wednesday)]
        [TestCase(DayOfWeek.Thursday)]
        [TestCase(DayOfWeek.Friday)]
        [TestCase(DayOfWeek.Saturday)]
        public void NextDateTime_CreatesDateTime_OnSpecifiedDayOfWeek(DayOfWeek dayOfWeek)
        {
            var min = new DateTime(2000, 01, 01);
            var max = new DateTime(2001, 01, 01);
            var dateTime = randomValueGenerator.NextDate(min, max, dayOfWeek);

            Assert.AreEqual(dayOfWeek, dateTime.DayOfWeek);
        }

        [Test]
        public void NextDateTime_ThrowsArgumentException_WhenMaxIsLessThanMin()
        {
            var min = new DateTime(2000, 01, 01);
            var max = min.AddDays(-1);
            Assert.Throws<ArgumentException>(() =>
                randomValueGenerator.NextDate(min, max, dayOfWeek: null)
            );
        }

        [Test]
        public void NextDateTime_ThrowsArgumentException_WhenRangeIsTooSmallToProduceDateTimeOnRequestedDayOfWeek()
        {
            var min = new DateTime(2019, 01, 15);
            var max = new DateTime(2019, 01, 20);
            Assert.Throws<ArgumentException>(() =>
                // no monday between these dates, so impossible to fulfill request
                randomValueGenerator.NextDate(min, max, DayOfWeek.Monday)
            );
        }

        [Test]
        public void NextPrice_PricesIsUpdatedEvenIfMaxPercentageDeviationIsLessThanMinPriceVariation()
        {
            // Default min price variation for crypto is 0.01
            var maximumPercentDeviation = 0.45m;
            var referencePrice = 2m;

            // The maximum price variation is 0.45% of 2, which is 0.009, less than the minimum price variation of 0.01.
            // The generated price will be rounded back to 2m, but this should be properly handled.

            var price = randomValueGenerator.NextPrice(SecurityType.Crypto, Market.GDAX, referencePrice, maximumPercentDeviation);

            Assert.AreNotEqual(referencePrice, price);
        }
    }
}
