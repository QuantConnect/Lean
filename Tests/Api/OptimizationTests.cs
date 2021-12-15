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
using NUnit.Framework;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
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
                nodeType: "O2-8",
                parallelNodes: 12
            );

            Assert.IsNotNull(optimization);
        }

        [Test]
        public void ListOptimizations()
        {
            var optimizations = ApiClient.ListOptimizations(testProjectId);
            Assert.IsNotNull(optimizations);
        }

        [Test]
        public void ReadOptimization()
        {
            var optimization = ApiClient.ReadOptimization(testOptimizationId);
            Assert.IsNotNull(optimization);
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
    }
}
