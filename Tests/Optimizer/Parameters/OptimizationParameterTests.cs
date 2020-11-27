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
            private static TestCaseData[] SegmentBasedOptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100), 10),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 10, -100), 5),
                new TestCaseData(new OptimizationStepParameter("ema-fast", -1, -100), 4)
            };

            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m)),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m, 0.0005m))
            };

            [TestCase(5)]
            [TestCase(-5)]
            [TestCase(0.5)]
            [TestCase(-0.5)]
            public void StepShouldBePositiveAlways(double step)
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, new decimal(step));

                Assert.NotNull(optimizationParameter.Step);
                Assert.Positive(optimizationParameter.Step.Value);
                Assert.AreEqual(optimizationParameter.Step, optimizationParameter.MinStep);
                Assert.AreEqual(Math.Abs(step), optimizationParameter.Step);
            }

            [TestCase(1, 0.1)]
            [TestCase(5, -0.1)]
            [TestCase(-0.1, -0.1)]
            [TestCase(0.5, -10)]
            public void StepShouldBeGreatOrEqualThanMinStep(decimal step, decimal minStep)
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, step, minStep);

                var actual = Math.Max(Math.Abs(step), Math.Abs(minStep));
                Assert.AreEqual(actual, optimizationParameter.Step);
            }

            [TestCase(0, 0)]
            [TestCase(0.5, 0)]
            [TestCase(2, 0)]
            public void PreventZero(decimal step, decimal minStep)
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100, step, minStep);

                var actual = Math.Max(Math.Abs(step), Math.Abs(minStep));
                actual = actual == 0 ? 1 : actual;

                Assert.AreEqual(actual, optimizationParameter.Step);
                Assert.AreEqual(actual, optimizationParameter.MinStep);
            }

            [Test, TestCaseSource(nameof(SegmentBasedOptimizationParameters))]
            public void CalculateStep(OptimizationStepParameter optimizationParameter, int numberOfSegments)
            {
                Assert.IsNull(optimizationParameter.Step);
                Assert.IsNull(optimizationParameter.MinStep);

                optimizationParameter.CalculateStep(numberOfSegments);
                var actual = Math.Abs(optimizationParameter.MaxValue - optimizationParameter.MinValue) /
                    numberOfSegments;
                Assert.AreEqual(actual, optimizationParameter.Step);
                Assert.AreEqual(actual / 10, optimizationParameter.MinStep);
            }

            [TestCase(0)]
            [TestCase(-1)]
            public void ThrowExceptionIfNonPositive(int numberOfSegments)
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100);
                Assert.IsNull(optimizationParameter.Step);
                Assert.IsNull(optimizationParameter.MinStep);

                Assert.Throws<ArgumentException>(() =>
                {
                    optimizationParameter.CalculateStep(numberOfSegments);
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


            private static TestCaseData[] OptimizationParametersEstimations => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1), 100),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 50), 2),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 1, 0), 1),
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, -2, 1), 4)
            };

            [Test, TestCaseSource(nameof(OptimizationParametersEstimations))]
            public void Estimate(OptimizationStepParameter optimizationParameter, int estimation)
            {
                Assert.AreEqual(estimation, optimizationParameter.Estimate());
            }

            [Test]
            public void ThrowIfEstimateNoStep()
            {
                var optimizationParameter = new OptimizationStepParameter("ema-fast", 1, 100);
                Assert.Throws<InvalidOperationException>(() =>
                {
                    optimizationParameter.Estimate();
                });
            }
        }

        [TestFixture]
        public class ArrayParameter
        {
            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationArrayParameter("ema-fast", new[]{"1", "2", "3"}))
            };

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void Serialize(OptimizationArrayParameter parameterSet)
            {
                var json = JsonConvert.SerializeObject(parameterSet);
                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationArrayParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual(parameterSet.Name, optimizationParameter.Name);
                Assert.AreEqual(parameterSet.Values, optimizationParameter.Values);
            }

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void SerializeCollection(OptimizationArrayParameter parameterSet)
            {
                var json = JsonConvert.SerializeObject(new[] { parameterSet as OptimizationParameter });
                var optimizationParameters = JsonConvert.DeserializeObject<List<OptimizationParameter>>(json);

                Assert.AreEqual(1, optimizationParameters.Count);

                var parsed = optimizationParameters[0] as OptimizationArrayParameter;
                Assert.NotNull(parsed);
                Assert.AreEqual(parameterSet.Name, parsed.Name);
                Assert.AreEqual(parameterSet.Values, parsed.Values);

            }

            [Test]
            public void DeserializeSingle()
            {
                var json = @"
                    {
                        ""name"":""ema-fast"",
                        ""values"": [""a"",""b"",""c"",""d""]
                    }";

                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationArrayParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual("ema-fast", optimizationParameter.Name);
                Assert.AreEqual(new[] { "a", "b", "c", "d" }, optimizationParameter.Values);
            }

            [Test]
            public void DeserializeSingleCaseInsenssitive()
            {
                var json = @"
                    {
                        ""name"":""ema-fast"",
                        ""VALUES"": [""a"",""b"",""c"",""d""]
                    }";

                var optimizationParameter = JsonConvert.DeserializeObject<OptimizationParameter>(json) as OptimizationArrayParameter;

                Assert.NotNull(optimizationParameter);
                Assert.AreEqual("ema-fast", optimizationParameter.Name);
                Assert.AreEqual(new[] { "a", "b", "c", "d" }, optimizationParameter.Values);
            }

            [Test]
            public void DeserializeCollection()
            {
                var json = @"[
                    {
                        ""name"":""ema-fast"",
                        ""values"": [""a"",""b"",""c"",""d""]
                    },{
                        ""name"":""ema-slow"",
                        ""values"": [""1"",""2"",""3"",""4""]
                    }]";

                var optimizationParameters = JsonConvert.DeserializeObject<List<OptimizationParameter>>(json)
                    .OfType<OptimizationArrayParameter>()
                    .ToList();

                Assert.AreEqual(2, optimizationParameters.Count);

                Assert.AreEqual("ema-fast", optimizationParameters[0].Name);
                Assert.AreEqual(new[] { "a", "b", "c", "d" }, optimizationParameters[0].Values);
                Assert.AreEqual("ema-slow", optimizationParameters[1].Name);
                Assert.AreEqual(new[] { "1", "2", "3", "4" }, optimizationParameters[1].Values);
            }

            private static TestCaseData[] OptimizationParametersEstimations => new[]
            {
                new TestCaseData(new OptimizationArrayParameter("ema-fast", new[] {"1", "2","3"}), 3),
                new TestCaseData(new OptimizationArrayParameter("ema-fast", new[] {"a","b","c","d"}), 4)
            };

            [Test, TestCaseSource(nameof(OptimizationParametersEstimations))]
            public void Estimate(OptimizationArrayParameter optimizationParameter, int estimation)
            {
                Assert.AreEqual(estimation, optimizationParameter.Estimate());
            }

            private static TestCaseData[] OptimizationParametersEmptyEstimations => new[]
            {
                new TestCaseData(null),
                new TestCaseData(new List<string>())
            };

            [Test, TestCaseSource(nameof(OptimizationParametersEmptyEstimations))]
            public void ThrowIfEstimateNullOrEmpty(IList<string> values)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    var pepe = new OptimizationArrayParameter("ema-fast", values);
                });
            }
        }
    }
}
