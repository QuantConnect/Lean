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
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Statistics
{
    [TestFixture]
    public class KellyCriterionManagerTests
    {
        [TestCase(1)]
        [TestCase(0)]
        public void ExtremeCasesBigNumbers(int extremeCase)
        {
            var extremePositive = new decimal[] { 1000, 2000, 3000, 1000, 5000, 1000, 1000 };
            var extremeNegative = new decimal[] { -1000, -2000, -3000, -1000, -5000, -1000, -1000 };

            decimal[] collection = extremeCase == 1 ? extremePositive : extremeNegative;

            var kellyCriterionManager = new KellyCriterionManager();
            foreach (var newValue in collection)
            {
                kellyCriterionManager.AddNewValue(newValue, DateTime.UtcNow);
                kellyCriterionManager.UpdateScores();
            }
            var estimate = kellyCriterionManager.KellyCriterionEstimate;
            var probabilityValue = kellyCriterionManager.KellyCriterionProbabilityValue;

            Console.WriteLine($"Estimate {estimate} - ProbabilityValue {probabilityValue}");
            Assert.AreEqual(extremeCase == 1
                    ? 0.00031578947368421 : -0.00031578947368421, kellyCriterionManager.KellyCriterionEstimate);
            Assert.AreEqual(1, kellyCriterionManager.KellyCriterionProbabilityValue);
        }

        [TestCase(1)]
        [TestCase(0)]
        public void ExtremeCasesSmallNumbers(int extremeCase)
        {
            var extremePositive = new decimal[] { 1, 2, 3, 1, 5, 1, 1 };
            var extremeNegative = new decimal[] { -1, -2, -3, -1, -5, -1, -1 };

            decimal[] collection = extremeCase == 1 ? extremePositive : extremeNegative;

            var kellyCriterionManager = new KellyCriterionManager();
            foreach (var newValue in collection)
            {
                kellyCriterionManager.AddNewValue(newValue, DateTime.UtcNow);
                kellyCriterionManager.UpdateScores();
            }

            var estimate = kellyCriterionManager.KellyCriterionEstimate;
            var probabilityValue = kellyCriterionManager.KellyCriterionProbabilityValue;

            Console.WriteLine($"Estimate {estimate} - ProbabilityValue {probabilityValue}");
            Assert.AreEqual(extremeCase == 1
                ? 0.315789473684211m : -0.315789473684211m, kellyCriterionManager.KellyCriterionEstimate);
            Assert.AreEqual(1, kellyCriterionManager.KellyCriterionProbabilityValue);
        }

        [Test]
        public void MiddleCase()
        {
            // values sum up to 0 -> average will be 0
            var middleCase = new decimal[] { 1000, -2000, -3000, 1000, 5000, -1000, -1000 };

            var kellyCriterionManager = new KellyCriterionManager();
            foreach (var newValue in middleCase)
            {
                kellyCriterionManager.AddNewValue(newValue, DateTime.UtcNow);
                kellyCriterionManager.UpdateScores();
            }

            var estimate = kellyCriterionManager.KellyCriterionEstimate;
            var probabilityValue = kellyCriterionManager.KellyCriterionProbabilityValue;

            Console.WriteLine($"Estimate {estimate} - ProbabilityValue {probabilityValue}");

            Console.WriteLine($"Estimate {estimate} - ProbabilityValue {probabilityValue}");

            // compare with a delta
            Assert.Less(Math.Abs(0 - kellyCriterionManager.KellyCriterionEstimate), 0.000000000000001m);
            Assert.AreEqual(1, kellyCriterionManager.KellyCriterionProbabilityValue);
        }

        [Test]
        public void RemovesOldValues()
        {
            var kellyCriterionManager = new KellyCriterionManager();

            var start = new DateTime(2019, 1, 1);
            kellyCriterionManager.AddNewValue(10, start);
            kellyCriterionManager.AddNewValue(10, start);
            kellyCriterionManager.UpdateScores();

            Console.WriteLine($"Estimate {kellyCriterionManager.KellyCriterionEstimate}" +
                              $" - ProbabilityValue {kellyCriterionManager.KellyCriterionProbabilityValue}");
            Assert.AreEqual(0.1, kellyCriterionManager.KellyCriterionEstimate);
            Assert.AreEqual(0.5, kellyCriterionManager.KellyCriterionProbabilityValue);


            kellyCriterionManager.AddNewValue(-10, start.AddDays(365));
            kellyCriterionManager.AddNewValue(-10, start.AddDays(365));
            kellyCriterionManager.UpdateScores();

            Console.WriteLine($"Estimate {kellyCriterionManager.KellyCriterionEstimate}" +
                              $" - ProbabilityValue {kellyCriterionManager.KellyCriterionProbabilityValue}");
            Assert.AreEqual(-0.1, kellyCriterionManager.KellyCriterionEstimate);
            Assert.AreEqual(0.5, kellyCriterionManager.KellyCriterionProbabilityValue);
        }
    }
}
