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
            new TestCaseData(new OptimizationStepParameter("ema-slow", 100, 1000, 10m))
        };

        [Test, TestCaseSource(nameof(OptimizationParameters))]
        public void NotIEnumerable(OptimizationParameter optimizationParameter)
        {
            Assert.IsNotInstanceOf<IEnumerable<string>>(optimizationParameter);
        }

        [TestFixture]
        public class StepParameter
        {
            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", -100, 0, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", -10, 10, 0.1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 100, 100, 0.5m))
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
        }
    }
}
