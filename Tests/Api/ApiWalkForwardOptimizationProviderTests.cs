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
using Moq;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using ApiOptimization = QuantConnect.Api.Optimization;
using ApiOptimizationSummary = QuantConnect.Api.OptimizationSummary;
using ApiWalkForwardOptimizationProvider = QuantConnect.Api.ApiWalkForwardOptimizationProvider;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    [NonParallelizable]
    public class ApiWalkForwardOptimizationProviderTests : ApiWalkForwardOptimizationProviderTestBase
    {
        [Test]
        public void RequiresExplicitCloudResourceConfiguration()
        {
            Config.Set("walk-forward-optimization-compile-id", string.Empty);
            var api = new Mock<IApi>(MockBehavior.Strict);
            var provider = new ApiWalkForwardOptimizationProvider(api.Object);

            Assert.Throws<InvalidOperationException>(() => provider.Optimize(CreateTargetRequest()));

            api.Verify(
                apiClient => apiClient.CreateOptimization(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<HashSet<OptimizationParameter>>(),
                    It.IsAny<IReadOnlyList<Constraint>>(),
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public void ThrowsWhenCloudOptimizationIsAborted()
        {
            var api = CreateApiForSingleRead(new ApiOptimization { Status = OptimizationStatus.Aborted });
            var provider = new ApiWalkForwardOptimizationProvider(api.Object);

            Assert.Throws<InvalidOperationException>(() => provider.Optimize(CreateTargetRequest()));
        }

        [Test]
        public void ThrowsWhenCloudOptimizationTimesOut()
        {
            Config.Set("walk-forward-optimization-api-timeout-minutes", "0");
            var api = CreateApiForSingleRead(new ApiOptimization { Status = OptimizationStatus.Running });
            var provider = new ApiWalkForwardOptimizationProvider(api.Object);

            Assert.Throws<TimeoutException>(() => provider.Optimize(CreateTargetRequest()));
            api.Verify(apiClient => apiClient.ReadOptimization("optimization-1"), Times.Once);
        }

        private static Mock<IApi> CreateApiForSingleRead(ApiOptimization optimization)
        {
            var api = new Mock<IApi>(MockBehavior.Strict);
            api.Setup(apiClient => apiClient.CreateOptimization(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<HashSet<OptimizationParameter>>(),
                    It.IsAny<IReadOnlyList<Constraint>>(),
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
                .Returns(new ApiOptimizationSummary { OptimizationId = "optimization-1" });
            api.Setup(apiClient => apiClient.ReadOptimization("optimization-1"))
                .Returns(optimization);

            return api;
        }
    }
}
