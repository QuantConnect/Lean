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
using QuantConnect.Optimizer;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class ExtremumTests
    {
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
