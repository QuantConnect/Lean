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


using System;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Optimizer.Parameters;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Optimizer.Parameters
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptimizationParameterTests
    {
        [TestFixture]
        public class StepParameter
        {
            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m, 0.0005m))
            };

            [TestCase(5)]
            [TestCase(0.5)]
            public void StepShouldBePositiveAlways(double step)
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, new decimal(step));

                Assert.NotNull(optimizationParameter.Step);
                Assert.Positive(optimizationParameter.Step.Value);
                Assert.AreEqual(optimizationParameter.Step, optimizationParameter.MinStep);
                Assert.AreEqual(Math.Abs(step), optimizationParameter.Step);
            }

            [TestCase(-5)]
            [TestCase(-0.5)]
            [TestCase(0)]
            public void ThrowIfStepIsNegativeOrZero(double step)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, new decimal(step));
                });
            }

            [TestCase(1, 0.1)]
            [TestCase(5, 5)]
            public void StepShouldBeGreatOrEqualThanMinStep(decimal step, decimal minStep)
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, step, minStep);

                var actual = Math.Max(Math.Abs(step), Math.Abs(minStep));
                Assert.AreEqual(actual, optimizationParameter.Step);
            }

            [TestCase(5, 10)]
            [TestCase(0.1, 0.2)]
            public void ThrowsIfStepLessThanMinStep(decimal step, decimal minStep)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, step, minStep);
                });
            }

            [TestCase(0, 0)]
            [TestCase(0.5, 0)]
            [TestCase(0, 2)]
            public void PreventZero(decimal step, decimal minStep)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, step, minStep);
                });
            }

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void Serialize(OptimizationStepParameter parameterSet)
            {
                var json = JsonConvert.SerializeObject(parameterSet);
                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationStepParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual(parameterSet.Name, optimizationParameter.Name);
                Assert.AreEqual(parameterSet.MinValue, optimizationParameter.MinValue);
                Assert.AreEqual(parameterSet.MaxValue, optimizationParameter.MaxValue);
                Assert.AreEqual(parameterSet.Step, optimizationParameter.Step);
                Assert.AreEqual(parameterSet.MinStep, optimizationParameter.MinStep);
            }

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void SerializeCollection(OptimizationStepParameter parameterSet)
            {
                var json = JsonConvert.SerializeObject(new[] { parameterSet as OptimizationParameter });
                var optimizationParameters = JsonConvert.DeserializeObject<List<OptimizationParameter>>(json);

                Assert.AreEqual(1, optimizationParameters.Count);

                var parsed = optimizationParameters[0] as OptimizationStepParameter;
                Assert.NotNull(parsed);
                Assert.AreEqual(parameterSet.Name, parsed.Name);
                Assert.AreEqual(parameterSet.MinValue, parsed.MinValue);
                Assert.AreEqual(parameterSet.MaxValue, parsed.MaxValue);
                Assert.AreEqual(parameterSet.Step, parsed.Step);
                Assert.AreEqual(parameterSet.MinStep, parsed.MinStep);
            }

            [Test]
            public void StaticParameterRoundTripSerialization()
            {
                var expected = "{\"value\":\"50.0\",\"name\":\"ema-fast\"}";

                var staticParameter = JsonConvert.DeserializeObject<StaticOptimizationParameter>(expected);

                Assert.IsNotNull(staticParameter);
                Assert.AreEqual("50.0", staticParameter.Value);
                Assert.AreEqual("ema-fast", staticParameter.Name);

                var serialized = JsonConvert.SerializeObject(staticParameter);

                Assert.AreEqual(expected, serialized);
            }

            [Test]
            public void DeserializeSingle()
            {
                var json = @"
                    {
                        ""name"":""ema-fast"",
                        ""min"": 50,
                        ""max"": 150,
                        ""step"": 50,
                    }";

                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationStepParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual("ema-fast", optimizationParameter.Name);
                Assert.AreEqual(50, optimizationParameter.MinValue);
                Assert.AreEqual(150, optimizationParameter.MaxValue);
                Assert.AreEqual(50, optimizationParameter.Step);
                Assert.AreEqual(50, optimizationParameter.MinStep);
            }

            [Test]
            public void DeserializeSingleCaseInsensitive()
            {
                var json = @"
                    {
                        ""Name"":""ema-fast"",
                        ""mIn"": 50,
                        ""maX"": 150,
                        ""STEP"": 50,
                    }";

                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationStepParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual("ema-fast", optimizationParameter.Name);
                Assert.AreEqual(50, optimizationParameter.MinValue);
                Assert.AreEqual(150, optimizationParameter.MaxValue);
                Assert.AreEqual(50, optimizationParameter.Step);
            }

            [Test]
            public void DeserializeSingleWithOptional()
            {
                var json = @"
                    {
                        ""name"":""ema-fast"",
                        ""min"": 50,
                        ""max"": 150,
                        ""step"": 50,
                        ""min-step"": 0.001
                    }";

                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationStepParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual("ema-fast", optimizationParameter.Name);
                Assert.AreEqual(50, optimizationParameter.MinValue);
                Assert.AreEqual(150, optimizationParameter.MaxValue);
                Assert.AreEqual(50, optimizationParameter.Step);
                Assert.AreEqual(0.001, optimizationParameter.MinStep);
            }

            [Test]
            public void DeserializeCollection()
            {
                var json = @"[
                    {
                        ""name"":""ema-fast"",
                        ""min"": 50,
                        ""max"": 150,
                        ""step"": 50,
                    },{
                        ""name"":""ema-slow"",
                        ""min"": 50,
                        ""max"": 250,
                        ""step"": 10,
                    }]";

                var optimizationParameters = JsonConvert.DeserializeObject<List<OptimizationParameter>>(json)
                    .OfType<OptimizationStepParameter>()
                    .ToList();

                Assert.AreEqual(2, optimizationParameters.Count);

                Assert.AreEqual("ema-fast", optimizationParameters[0].Name);
                Assert.AreEqual(50, optimizationParameters[0].MinValue);
                Assert.AreEqual(150, optimizationParameters[0].MaxValue);
                Assert.AreEqual(50, optimizationParameters[0].Step);
                Assert.AreEqual("ema-slow", optimizationParameters[1].Name);
                Assert.AreEqual(50, optimizationParameters[1].MinValue);
                Assert.AreEqual(250, optimizationParameters[1].MaxValue);
                Assert.AreEqual(10, optimizationParameters[1].Step);
            }

            [Test]
            public void ThrowIfNotStepParameter()
            {
                var json = @"
                    {
                        ""name"":""ema-fast"",
                        ""values"": [""a"",""b"",""c"",""d""]
                    }";

                Assert.Throws<ArgumentException>(() =>
                {
                    JsonConvert.DeserializeObject<OptimizationParameter>(json);
                });
            }

            [TestCase(-1, -10, -1)]
            [TestCase(10, 1, 1)]
            public void ThrowIfMinGreatThanMax(decimal min, decimal max, decimal step)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    var param = new OptimizationStepParameter("ema-fast", min, max, step);
                });
            }
        }
    }
}
