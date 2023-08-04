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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Statistics;
using QuantConnect.Util;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests API account and optimizations endpoints
    /// </summary>
    [TestFixture, Explicit("Requires configured api access")]
    public class OptimizationTests : ApiTestBase
    {
        private int testProjectId = 1; // "EnterProjectHere";
        private string testOptimizationId = "EnterOptimizationHere";
        
        private string _validSerialization = "{\"optimizationId\":\"myOptimizationId\",\"name\":\"myOptimizationName\",\"runtimeStatistics\":{\"Completed\":\"1\"},"+
            "\"constraints\":[{\"target\":\"TotalPerformance.PortfolioStatistics.SharpeRatio\",\"operator\":\"GreaterOrEqual\",\"target-value\":1}],"+
            "\"parameters\":[{\"name\":\"myParamName\",\"min\":2,\"max\":4,\"step\":1}],\"nodeType\":\"O2-8\",\"parallelNodes\":12,\"projectId\":1234567,\"status\":\"completed\","+
            "\"backtests\":{\"myBacktestKey\":{\"name\":\"myBacktestName\",\"id\":\"myBacktestId\",\"progress\":1,\"exitCode\":0,"+
            "\"statistics\":[0.374,0.217,0.047,-4.51,2.86,-0.664,52.602,17.800,6300000.00,0.196,1.571,27.0,123.888,77.188,0.63,1.707,1390.49,180.0,0.233,-0.558,73.0]," +
            "\"parameterSet\":{\"myParamName\":\"2\"},\"equity\":[]}},\"strategy\":\"QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy\"," +
            "\"requested\":\"2021-12-16 00:51:58\",\"criterion\":{\"extremum\":\"max\",\"target\":\"TotalPerformance.PortfolioStatistics.SharpeRatio\",\"targetValue\":null}}";

        private string _validEstimateSerialization = "{\"estimateId\":\"myEstimateId\",\"time\":26,\"balance\":500}";

        [Test]
        public void Deserialization()
        {
            var deserialized = JsonConvert.DeserializeObject<Optimization>(_validSerialization);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual("myOptimizationId", deserialized.OptimizationId);
            Assert.AreEqual("myOptimizationName", deserialized.Name);
            Assert.IsTrue(deserialized.RuntimeStatistics.Count == 1);
            Assert.IsTrue(deserialized.RuntimeStatistics["Completed"] == "1");
            Assert.IsTrue(deserialized.Constraints.Count == 1);
            Assert.AreEqual("['TotalPerformance'].['PortfolioStatistics'].['SharpeRatio']", deserialized.Constraints[0].Target);
            Assert.IsTrue(deserialized.Constraints[0].Operator == ComparisonOperatorTypes.GreaterOrEqual);
            Assert.IsTrue(deserialized.Constraints[0].TargetValue == 1);
            Assert.IsTrue(deserialized.Parameters.Count == 1);
            var stepParam = deserialized.Parameters.First().ConvertInvariant<OptimizationStepParameter>();
            Assert.IsTrue(stepParam.Name == "myParamName");
            Assert.IsTrue(stepParam.MinValue == 2);
            Assert.IsTrue(stepParam.MaxValue == 4);
            Assert.IsTrue(stepParam.Step == 1);
            Assert.AreEqual(OptimizationNodes.O2_8, deserialized.NodeType);
            Assert.AreEqual(12, deserialized.ParallelNodes);
            Assert.AreEqual(1234567, deserialized.ProjectId);
            Assert.AreEqual(OptimizationStatus.Completed, deserialized.Status);
            Assert.IsTrue(deserialized.Backtests.Count == 1);
            Assert.IsTrue(deserialized.Backtests["myBacktestKey"].BacktestId == "myBacktestId");
            Assert.IsTrue(deserialized.Backtests["myBacktestKey"].Name == "myBacktestName");
            Assert.IsTrue(deserialized.Backtests["myBacktestKey"].ParameterSet.Value["myParamName"] == "2");
            Assert.IsTrue(deserialized.Backtests["myBacktestKey"].Statistics[PerformanceMetrics.ProbabilisticSharpeRatio] == "77.188");
            Assert.AreEqual("QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy", deserialized.Strategy);
            Assert.AreEqual(new DateTime(2021, 12, 16, 00, 51, 58), deserialized.Requested);
            Assert.AreEqual("['TotalPerformance'].['PortfolioStatistics'].['SharpeRatio']", deserialized.Criterion.Target);
            Assert.IsInstanceOf<Maximization>(deserialized.Criterion.Extremum);
            Assert.IsNull(deserialized.Criterion.TargetValue);
        }

        [Test]
        public void EstimateDeserialization()
        {
            var deserialized = JsonConvert.DeserializeObject<Estimate>(_validEstimateSerialization);
            Assert.AreEqual("myEstimateId", deserialized.EstimateId);
            Assert.AreEqual(26, deserialized.Time);
            Assert.AreEqual(500, deserialized.Balance);
        }

        [Test]
        public void EstimateOptimization()
        {
            var compile = ApiClient.CreateCompile(testProjectId);

            var estimate = ApiClient.EstimateOptimization(
                projectId: testProjectId,
                name: "My Testable Optimization",
                target: "TotalPerformance.PortfolioStatistics.SharpeRatio",
                targetTo: "max",
                targetValue: null,
                strategy: "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy",
                compileId: compile.CompileId,
                parameters: new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("atrRatioTP", 4, 5, 1, 1) // Replace params with valid optimization parameter data for test project
                },
                constraints: new List<Constraint>
                {
                    new Constraint("TotalPerformance.PortfolioStatistics.SharpeRatio", ComparisonOperatorTypes.GreaterOrEqual, 1)
                }
            );

            Assert.IsNotNull(estimate);
            Assert.IsNotEmpty(estimate.EstimateId);
            Assert.GreaterOrEqual(estimate.Time, 0);
            Assert.GreaterOrEqual(estimate.Balance, 0);
        }

        [Test]
        public void CreateOptimization()
        {
            var compile = ApiClient.CreateCompile(testProjectId);

            var optimization = ApiClient.CreateOptimization(
                projectId: testProjectId,
                name: "My Testable Optimization",
                target: "TotalPerformance.PortfolioStatistics.SharpeRatio",
                targetTo: "max",
                targetValue: null,
                strategy: "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy",
                compileId: compile.CompileId,
                parameters: new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("atrRatioTP", 4, 5, 1, 1) // Replace params with valid optimization parameter data for test project
                },
                constraints: new List<Constraint>
                {
                    new Constraint("TotalPerformance.PortfolioStatistics.SharpeRatio", ComparisonOperatorTypes.GreaterOrEqual, 1)
                },
                estimatedCost: 0.06m,
                nodeType: OptimizationNodes.O2_8,
                parallelNodes: 12
            );

            TestBaseOptimization(optimization);
        }

        [Test]
        public void ListOptimizations()
        {
            var optimizations = ApiClient.ListOptimizations(testProjectId);
            Assert.IsNotNull(optimizations);
            Assert.IsTrue(optimizations.Any());
            TestBaseOptimization(optimizations.First());
        }

        [Test]
        public void ReadOptimization()
        {
            var optimization = ApiClient.ReadOptimization(testOptimizationId);

            TestBaseOptimization(optimization);
            Assert.IsTrue(optimization.RuntimeStatistics.Any());
            Assert.IsTrue(optimization.Constraints.Any());
            Assert.IsTrue(optimization.Parameters.Any());
            Assert.Positive(optimization.ParallelNodes);
            Assert.IsTrue(optimization.Backtests.Any());
            Assert.IsNotEmpty(optimization.Strategy);
            Assert.AreNotEqual(default(DateTime), optimization.Requested);
        }

        [Test]
        public void AbortOptimization()
        {
            var response = ApiClient.AbortOptimization(testOptimizationId);
            Assert.IsTrue(response.Success);
        }

        [Test]
        public void UpdateOptimization()
        {
            var response = ApiClient.UpdateOptimization(testOptimizationId, "Alert Yellow Submarine");
            Assert.IsTrue(response.Success);
        }

        [Test]
        public void DeleteOptimization()
        {
            var response = ApiClient.DeleteOptimization(testOptimizationId);
            Assert.IsTrue(response.Success);
        }

        private static void TestBaseOptimization(BaseOptimization optimization)
        {
            Assert.IsNotNull(optimization);
            Assert.IsNotEmpty(optimization.OptimizationId);
            Assert.Positive(optimization.ProjectId);
            Assert.IsNotEmpty(optimization.Name);
            Assert.IsInstanceOf<OptimizationStatus>(optimization.Status);
            Assert.IsNotEmpty(optimization.NodeType);
            Assert.IsNotNull(optimization.Criterion);
        }
    }
}
