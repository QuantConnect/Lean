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


using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Optimizer;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture]
    public class OptimizationParameterTests
    {
        [TestFixture]
        public class Step
        {
            private static TestCaseData[] OptimizationParameters => new[]
            {
                new TestCaseData(new OptimizationStepParameter("ema-fast", 1, 100, 1m))
            };

            [Test, TestCaseSource(nameof(OptimizationParameters))]
            public void Serialize(OptimizationStepParameter parameterSet)
            {
                var json = JsonConvert.SerializeObject(parameterSet);
                var optimizationParameters = JsonConvert.DeserializeObject<Dictionary<string, OptimizationParameter>>(json);

                Assert.AreEqual(1, optimizationParameters.Count);
                Assert.Contains(parameterSet.Name, optimizationParameters.Keys);

                var parsed = optimizationParameters[parameterSet.Name] as OptimizationStepParameter;
                Assert.NotNull(parsed);
                Assert.AreEqual(parameterSet.Name, parsed.Name);
                Assert.AreEqual(parameterSet.MinValue, parsed.MinValue);
                Assert.AreEqual(parameterSet.MaxValue, parsed.MaxValue);
                Assert.AreEqual(parameterSet.Step, parsed.Step);

            }

            [Test]
            public void Deserialize()
            {
                var json = @"{
                    ""ema-slow"": {
                        ""min"": 10,
                        ""max"": 50,
                        ""step"": 10,
                        
                        // optional
                        ""min-step"": 0.5
                    },
                    ""ema-fast"": {
                        ""min"": 50,
                        ""max"": 150,
                        ""step"": 50,

                        // optional
                        ""min-step"": 0.0001
                    }
                }";

                var optimizationParameters = JsonConvert.DeserializeObject<Dictionary<string, OptimizationParameter>>(json);

                Assert.AreEqual(2, optimizationParameters.Count);
                foreach (var entry in optimizationParameters)
                {
                    Assert.IsInstanceOf<OptimizationStepParameter>(entry.Value);
                    Assert.AreEqual(entry.Key, entry.Value.Name);
                }
            }
        }
    }
}
