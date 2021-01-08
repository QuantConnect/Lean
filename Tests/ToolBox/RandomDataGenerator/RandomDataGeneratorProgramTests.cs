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
using NUnit.Framework;
using QuantConnect.ToolBox.RandomDataGenerator;

namespace QuantConnect.Tests.ToolBox.RandomDataGenerator
{
    [TestFixture]
    public class RandomDataGeneratorProgramTests
    {
        [Test]
        [TestCase("2020, 1, 1 00:00:00", "2020, 1, 1 00:00:00", "2020, 1, 1 00:00:00")]
        [TestCase("2020, 1, 1 00:00:00", "2020, 2, 1 00:00:00", "2020, 1, 16 12:00:00")] // (31 days / 2) = 15.5 = 16 Rounds up to 12 pm
        [TestCase("2020, 1, 1 00:00:00", "2020, 3, 1 00:00:00", "2020, 1, 31 00:00:00")] // (60 days / 2) = 30
        [TestCase("2020, 1, 1 00:00:00", "2020, 6, 1 00:00:00", "2020, 3, 17 00:00:00")] // (152 days / 2) = 76

        public void NextRandomGeneratedData(DateTime start, DateTime end, DateTime expectedMidPoint)
        {
            var randomValueGenerator = new RandomValueGenerator();
            var midPoint = RandomDataGeneratorProgram.GetDateMidpoint(start, end);
            var delistDate = RandomDataGeneratorProgram.GetDelistingDate(start, end, randomValueGenerator);

            // midPoint and expectedMidPoint must be the same
            Assert.AreEqual(expectedMidPoint, midPoint);

            // start must be less than or equal to end
            Assert.LessOrEqual(start, end);

            // delistDate must be less than or equal to end
            Assert.LessOrEqual(delistDate, end);
            Assert.GreaterOrEqual(delistDate, midPoint);
        }
    }
}