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

using NUnit.Framework;
using QuantConnect.Optimizer.Parameters;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Optimizer.Parameters
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptimizationParameterEnumeratorTests
    {
        private static TestCaseData[] OptimizationParameters => new[]
        {
            new TestCaseData(new OptimizationStepParameter("ema-fast", -100, 0, 1m)),
            new TestCaseData(new OptimizationArrayParameter("ema-fast", new []{"0", "1", "2", "3"}))
        };

        [Test, TestCaseSource(nameof(OptimizationParameters))]
        public void IEnumerable(OptimizationParameter optimizationParameter)
        {
            Assert.IsInstanceOf<IEnumerable<string>>(optimizationParameter);
        }

        [TestFixture]
        public class StepParameter
        {
            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", -100, 0, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 10, -10, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 10, -10, 0)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 100, 100, 1m))
            };

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void Enumerate(OptimizationStepParameter optimizationParameter)
            {
                var enumerator = new OptimizationStepParameterEnumerator(optimizationParameter);
                int total = 0;

                for (decimal value = optimizationParameter.MinValue; value <= optimizationParameter.MaxValue; value += optimizationParameter.Step.Value)
                {
                    total++;
                    Assert.IsTrue(enumerator.MoveNext());
                    Assert.AreEqual(value, enumerator.Current.ToDecimal());
                }

                Assert.AreEqual(Math.Floor((optimizationParameter.MaxValue - optimizationParameter.MinValue) / optimizationParameter.Step.Value) + 1, total);
                Assert.IsFalse(enumerator.MoveNext());
            }

            [Test]
            public void IEnumerable()
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", -100, 0, 1m);
                Assert.IsInstanceOf<OptimizationStepParameterEnumerator>(optimizationParameter.GetEnumerator());
            }
        }

        [TestFixture]
        public class ArrayParameter
        {
            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationArrayParameter("ema-fast", new []{"0", "1", "2", "3"})),
                new TestCaseData(new OptimizationArrayParameter("ema-fast", new []{"a", null, "c", "d"}))
            };

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void Enumerate(OptimizationArrayParameter optimizationParameter)
            {
                var enumerator = new OptimizationArrayParameterEnumerator(optimizationParameter);

                using (var actual = optimizationParameter.Values.GetEnumerator())
                {
                    int total = 0;
                    while (enumerator.MoveNext())
                    {
                        total++;
                        Assert.IsTrue(actual.MoveNext());
                        Assert.AreEqual(actual.Current, enumerator.Current);
                    }

                    Assert.AreEqual(optimizationParameter.Values.Count, total);
                    Assert.IsFalse(actual.MoveNext());
                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            [Test]
            public void IEnumerable()
            {
                var optimizationParameter = new OptimizationArrayParameter("ema-fast", new[] { "1", "2", "3" });
                Assert.IsInstanceOf<OptimizationArrayParameterEnumerator>(optimizationParameter.GetEnumerator());
            }
        }
    }
}
