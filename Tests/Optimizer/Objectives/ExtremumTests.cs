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
using QuantConnect.Optimizer.Objectives;
using System;
using Newtonsoft.Json;

namespace QuantConnect.Tests.Optimizer.Objectives
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ExtremumTests
    {
        private static TestCaseData[] Extremums => new TestCaseData[]
        {
            new TestCaseData(new Maximization()),
            new TestCaseData(new Minimization())
        };


        [TestCase("\"max\"")]
        [TestCase("\"min\"")]
        [TestCase("\"Max\"")]
        [TestCase("\"miN\"")]
        public void Deserialize(string extremum)
        {
            var actual = JsonConvert.DeserializeObject<Extremum>(extremum);
            Extremum expected = extremum.Equals("\"max\"", StringComparison.OrdinalIgnoreCase)
                ? new Maximization() as Extremum
                : new Minimization();

            Assert.NotNull(actual);
            Assert.AreEqual(expected.GetType(), actual.GetType());
        }

        [TestCase("\"\"")]
        [TestCase("\"n/a\"")]
        public void ThrowsIfNotRecognized(string extremum)
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                JsonConvert.DeserializeObject<Extremum>(extremum);
            });
        }

        [Test, TestCaseSource(nameof(Extremums))]
        public void Serialize(Extremum extremum)
        {
            var json = JsonConvert.SerializeObject(extremum);

            var actual = JsonConvert.DeserializeObject<Extremum>(json);
            Assert.NotNull(actual);
            Assert.AreEqual(extremum.GetType(), actual.GetType());
        }

        [TestCase(0, 10)]
        [TestCase(10, 10)]
        [TestCase(10, 0)]
        public void Linear(decimal current, decimal candidate)
        {
            Func<decimal, decimal, bool> comparer = (a, b) => a <= b;
            var strategy = new Extremum(comparer);

            Assert.AreEqual(comparer(current, candidate), strategy.Better(current, candidate));
        }

        [TestCase(101, 10)]
        [TestCase(100, 10)]
        [TestCase(99, 10)]
        public void NonLinear(decimal current, decimal candidate)
        {
            Func<decimal, decimal, bool> comparer = (a, b) => a >= b * b;
            var strategy = new Extremum(comparer);

            Assert.AreEqual(comparer(current, candidate), strategy.Better(current, candidate));
        }

        [TestFixture]
        public class MaximizationTests
        {
            Maximization _strategy = new Maximization();

            [Test]
            public void Greater()
            {
                Assert.IsTrue(_strategy.Better(0, 10));
            }

            [TestCase(10, 10)]
            [TestCase(10, 0)]
            public void LessThanOrEqual(decimal current, decimal candidate)
            {
                Assert.IsFalse(_strategy.Better(current, candidate));
            }
        }

        [TestFixture]
        public class MinimizationTests
        {
            Minimization _strategy = new Minimization();

            [Test]
            public void Less()
            {
                Assert.IsTrue(_strategy.Better(10, 0));
            }

            [TestCase(10, 10)]
            [TestCase(0, 10)]
            public void GreatThanOrEqual(decimal current, decimal candidate)
            {
                Assert.IsFalse(_strategy.Better(current, candidate));
            }
        }
    }
}
