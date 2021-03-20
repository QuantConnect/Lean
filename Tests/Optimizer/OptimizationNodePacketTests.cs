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
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class OptimizationNodePacketTest
    {
        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture };

        [TestCase("QuantConnect.Optimizer.EulerSearchOptimizationStrategy", typeof(StepBaseOptimizationStrategySettings))]
        [TestCase("QuantConnect.Optimizer.GridSearchOptimizationStrategy", typeof(OptimizationStrategySettings))]
        public void RoundTrip(string strategyName, Type settingType)
        {
            var optimizationNodePacket = new OptimizationNodePacket()
            {
                CompileId = Guid.NewGuid().ToString(),
                OptimizationId = Guid.NewGuid().ToString(),
                OptimizationStrategy = strategyName,
                Criterion = new Target("Profit", new Maximization(), 100.5m),
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.1m),
                    new Constraint("Profit", ComparisonOperatorTypes.Greater, 100)
                },
                OptimizationParameters = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-slow", 1, 100, 1m),
                    new OptimizationStepParameter("ema-fast", -10, 0, 0.5m),
                    new StaticOptimizationParameter("pinocho", "11")
                },
                MaximumConcurrentBacktests = 10,
                OptimizationStrategySettings = (OptimizationStrategySettings)Activator.CreateInstance(settingType)
            };
            var serialize = JsonConvert.SerializeObject(optimizationNodePacket, _jsonSettings);
            var result = JsonConvert.DeserializeObject<OptimizationNodePacket>(serialize, _jsonSettings);

            // common
            Assert.AreEqual(PacketType.OptimizationNode, result.Type);

            // assert strategy
            Assert.AreEqual(optimizationNodePacket.OptimizationStrategy, result.OptimizationStrategy);
            Assert.AreEqual(optimizationNodePacket.OptimizationId, result.OptimizationId);
            Assert.AreEqual(optimizationNodePacket.CompileId, result.CompileId);

            // assert optimization parameters
            foreach (var expected in optimizationNodePacket.OptimizationParameters.OfType<OptimizationStepParameter>())
            {
                var actual = result.OptimizationParameters.FirstOrDefault(s => s.Name == expected.Name) as OptimizationStepParameter;
                Assert.NotNull(actual);
                Assert.AreEqual(expected.MinValue, actual.MinValue);
                Assert.AreEqual(expected.MaxValue, actual.MaxValue);
                Assert.AreEqual(expected.Step, actual.Step);
            }

            // assert optimization parameters
            var staticOptimizationParameters = optimizationNodePacket.OptimizationParameters.OfType<StaticOptimizationParameter>().ToList();
            Assert.AreEqual(1, staticOptimizationParameters.Count);
            foreach (var parameter in staticOptimizationParameters)
            {
                Assert.AreEqual("11", parameter.Value);
                Assert.AreEqual("pinocho", parameter.Name);
            }

            // assert target
            Assert.AreEqual(optimizationNodePacket.Criterion.Target, result.Criterion.Target);
            Assert.AreEqual(optimizationNodePacket.Criterion.Extremum.GetType(), result.Criterion.Extremum.GetType());
            Assert.AreEqual(optimizationNodePacket.Criterion.TargetValue, result.Criterion.TargetValue);

            // assert constraints
            foreach (var expected in optimizationNodePacket.Constraints)
            {
                var actual = result.Constraints.FirstOrDefault(s => s.Target == expected.Target);
                Assert.NotNull(actual);
                Assert.AreEqual(expected.Operator, actual.Operator);
                Assert.AreEqual(expected.TargetValue, actual.TargetValue);
            }

            // others
            Assert.AreEqual(optimizationNodePacket.MaximumConcurrentBacktests, result.MaximumConcurrentBacktests);
            Assert.AreEqual(settingType, result.OptimizationStrategySettings.GetType());
        }

        [TestCase("StepBaseOptimizationStrategySettings")]
        [TestCase("OptimizationStrategySettings")]
        public void FromJson(string settingTypeName)
        {
            var json = "{\"OptimizationParameters\": [{\"Name\":\"sleep-ms\",\"min\":0,\"max\":0,\"Step\":1}," +
                       "{\"Name\":\"total-trades\",\"min\":0,\"max\":2,\"Step\":1}]," +
                       "\"criterion\": {\"extremum\" : \"min\",\"target\": \"Statistics.Sharpe Ratio\",\"target-value\": 10}," +
                       "\"constraints\": [{\"target\": \"Statistics.Sharpe Ratio\",\"operator\": \"equals\",\"target-value\": 11}],"+
                       $"\"optimizationStrategySettings\":{{\"$type\":\"QuantConnect.Optimizer.Strategies.{settingTypeName}, QuantConnect.Optimizer\",\"default-segment-amount\":0,\"max-run-time\":\"10675199.02:48:05.4775807\"}}}}";

            var packet = (OptimizationNodePacket)JsonConvert.DeserializeObject(json, typeof(OptimizationNodePacket));

            Assert.AreEqual("['Statistics'].['Sharpe Ratio']", packet.Criterion.Target);
            Assert.AreEqual(typeof(Minimization), packet.Criterion.Extremum.GetType());
            Assert.AreEqual(2, packet.OptimizationParameters.Count);
            Assert.AreEqual(1, packet.Constraints.Count);
            Assert.AreEqual("['Statistics'].['Sharpe Ratio']", packet.Constraints.Single().Target);
            Assert.AreEqual(settingTypeName, packet.OptimizationStrategySettings.GetType().Name);
        }

        [Test]
        public void FromJsonNoSettings()
        {
            var json = "{\"OptimizationParameters\": [{\"Name\":\"sleep-ms\",\"min\":0,\"max\":0,\"Step\":1}," +
                       "{\"Name\":\"total-trades\",\"min\":0,\"max\":2,\"Step\":1}]," +
                       "\"criterion\": {\"extremum\" : \"min\",\"target\": \"Statistics.Sharpe Ratio\",\"target-value\": 10}," +
                       "\"constraints\": [{\"target\": \"Statistics.Sharpe Ratio\",\"operator\": \"equals\",\"target-value\": 11}]}";

            var packet = (OptimizationNodePacket)JsonConvert.DeserializeObject(json, typeof(OptimizationNodePacket));

            Assert.AreEqual("['Statistics'].['Sharpe Ratio']", packet.Criterion.Target);
            Assert.AreEqual(typeof(Minimization), packet.Criterion.Extremum.GetType());
            Assert.AreEqual(2, packet.OptimizationParameters.Count);
            Assert.AreEqual(1, packet.Constraints.Count);
            Assert.AreEqual("['Statistics'].['Sharpe Ratio']", packet.Constraints.Single().Target);
            Assert.IsNull(packet.OptimizationStrategySettings);
        }
    }
}
