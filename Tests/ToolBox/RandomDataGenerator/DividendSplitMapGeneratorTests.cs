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
    public class DividendSplitMapGeneratorTests
    {
        [TestCase(240, 0.9857119009006162)]
        [TestCase(120, 0.9716279515771061)]
        [TestCase(60, 0.9440608762859234)]
        [TestCase(12, 0.7498942093324559)]
        [TestCase(6, 0.5623413251903491)]
        [TestCase(3, 0.31622776601683794)]
        [TestCase(1, 0.03162277660168379)]
        public void GetsExpectedLowerBound(int months, double expectedLowerBound)
        {
            var lowerBound = (double)DividendSplitMapGenerator.GetLowerBoundForPreviousSplitFactor(months);
            Assert.AreEqual(expectedLowerBound, lowerBound, delta: 0.0000000000001);
            Assert.IsTrue(Math.Pow(lowerBound, lowerBound * 2) >= 0.0009);
        }

        [TestCase(240)]
        [TestCase(120)]
        [TestCase(60)]
        [TestCase(12)]
        [TestCase(6)]
        [TestCase(3)]
        [TestCase(1)]
        public void GetsValidNextPreviousSplitFactor(int months)
        {
            var lowerBound = DividendSplitMapGenerator.GetLowerBoundForPreviousSplitFactor(months);
            var upperBound = 1;
            var nextPreviousSplitFactor = DividendSplitMapGenerator.GetNextPreviousSplitFactor(new Random(), lowerBound, upperBound);
            Assert.IsTrue(lowerBound <= nextPreviousSplitFactor && nextPreviousSplitFactor <= upperBound);
            Assert.IsTrue(0.001 <= Math.Pow((double)nextPreviousSplitFactor, 2 * (double)months) && Math.Pow((double)nextPreviousSplitFactor, 2 * (double)months) <= 1);
        }

        [TestCase(240)]
        [TestCase(120)]
        [TestCase(60)]
        [TestCase(12)]
        [TestCase(6)]
        [TestCase(3)]
        [TestCase(1)]
        public void PriceScaledBySplitFactorIsBounded(int months)
        {
            var maxPossiblePrice = 1000000m;
            var minPossiblePrice = 0.0001m;
            var lowerBound = DividendSplitMapGenerator.GetLowerBoundForPreviousSplitFactor(months);
            var upperBound = 1;
            var nextPreviousSplitFactor = DividendSplitMapGenerator.GetNextPreviousSplitFactor(new Random(), lowerBound, upperBound);
            var finalSplitFactor = (decimal)Math.Pow((double)nextPreviousSplitFactor, 2 * (double)months);
            Assert.IsTrue(0.0001m <= (maxPossiblePrice / finalSplitFactor) && (maxPossiblePrice / finalSplitFactor) <= 1000000000m, (maxPossiblePrice / finalSplitFactor).ToString());
            Assert.IsTrue(0.0001m <= (minPossiblePrice / finalSplitFactor) && (minPossiblePrice / finalSplitFactor) <= 1000000000m, (minPossiblePrice / finalSplitFactor).ToString());
        }
    }
}
