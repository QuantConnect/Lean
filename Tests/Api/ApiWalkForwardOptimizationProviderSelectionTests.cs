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
using Moq;
using NUnit.Framework;
using QuantConnect.Interfaces;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Optimizer.Strategies;
using QuantConnect.Statistics;
using ApiOptimization = QuantConnect.Api.Optimization;
using ApiOptimizationNodes = QuantConnect.Api.OptimizationNodes;
using ApiOptimizationSummary = QuantConnect.Api.OptimizationSummary;
using ApiWalkForwardOptimizationProvider = QuantConnect.Api.ApiWalkForwardOptimizationProvider;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    [NonParallelizable]
    public class ApiWalkForwardOptimizationProviderSelectionTests : ApiWalkForwardOptimizationProviderTestBase
    {
        [Test]
        public void CreatesCloudOptimizationAndReturnsWinningParameterSet()
        {
            var request = CreateTargetRequest();
            var api = CreateApiForCompletedOptimization(request, "['Statistics'].['Net Profit']", "max", CreateCompletedOptimization());
            var provider = new ApiWalkForwardOptimizationProvider(api.Object);

            var result = provider.Optimize(request);

            Assert.AreEqual("20", result.ParameterSet.Value["ema-fast"]);
            Assert.AreEqual(2, result.Backtests.Count);
            api.Verify(apiClient => apiClient.ReadOptimization("optimization-1"), Times.Exactly(2));
        }

        [Test]
        public void MinimizationTargetsSelectLowestBacktestStatistic()
        {
            var request = CreateTargetRequest(new Target(PerformanceMetrics.Drawdown, new Minimization(), null));
            var api = CreateApiForCompletedOptimization(
                request,
                "['Statistics'].['Drawdown']",
                "min",
                CreateCompletedOptimization(PerformanceMetrics.Drawdown, 10, 5),
                includeRunningRead: false);
            var provider = new ApiWalkForwardOptimizationProvider(api.Object);

            var result = provider.Optimize(request);

            Assert.AreEqual("20", result.ParameterSet.Value["ema-fast"]);
        }

        [Test]
        public void SelectorRequestsReturnBacktestsWithoutPreselectedParameterSet()
        {
            var request = CreateSelectorRequest();
            var api = CreateApiForCompletedOptimization(request, "['Statistics'].['Net Profit']", "max", CreateCompletedOptimization(), false);
            var provider = new ApiWalkForwardOptimizationProvider(api.Object);

            var result = provider.Optimize(request);

            Assert.IsNull(result.ParameterSet);
            Assert.AreEqual(2, result.Backtests.Count);
            Assert.AreEqual("20", request.TargetSelector(result.Backtests).Value["ema-fast"]);
        }

        private static Mock<IApi> CreateApiForCompletedOptimization(
            WalkForwardOptimizationRequest request,
            string target,
            string targetTo,
            ApiOptimization completedOptimization,
            bool includeRunningRead = true)
        {
            var api = new Mock<IApi>(MockBehavior.Strict);
            api.Setup(apiClient => apiClient.CreateOptimization(
                    request.Algorithm.ProjectId,
                    It.Is<string>(name => name.StartsWith("Walk Forward Optimization", StringComparison.Ordinal)),
                    target,
                    targetTo,
                    null,
                    typeof(GridSearchOptimizationStrategy).FullName,
                    CompileId,
                    request.Parameters,
                    request.Constraints,
                    0.25m,
                    ApiOptimizationNodes.O2_8,
                    2))
                .Returns(new ApiOptimizationSummary { OptimizationId = "optimization-1" });

            if (includeRunningRead)
            {
                api.SetupSequence(apiClient => apiClient.ReadOptimization("optimization-1"))
                    .Returns(new ApiOptimization { Status = OptimizationStatus.Running })
                    .Returns(completedOptimization);
            }
            else
            {
                api.Setup(apiClient => apiClient.ReadOptimization("optimization-1"))
                    .Returns(completedOptimization);
            }

            return api;
        }
    }
}
