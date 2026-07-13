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

using System.Reflection;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Server;
using QuantConnect.Optimizer;
using QuantConnect.Packets;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class WalkForwardOptimizationProviderRoutingTests
    {
        [SetUp]
        public void SetUp()
        {
            Config.Set("walk-forward-optimization-provider", nameof(TestWalkForwardOptimizationProvider));
            Composer.Instance.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Config.Set("walk-forward-optimization-provider", nameof(NullWalkForwardOptimizationProvider));
            Composer.Instance.Reset();
        }

        [Test]
        public void LocalLeanManagerInjectsConfiguredWalkForwardOptimizationProvider()
        {
            var manager = new LocalLeanManager();
            var systemHandlers = new LeanEngineSystemHandlers(
                Mock.Of<IJobQueueHandler>(),
                new QuantConnect.Api.Api(),
                Mock.Of<IMessagingHandler>(),
                Mock.Of<ILeanManager>());
            manager.Initialize(systemHandlers, null, new BacktestNodePacket(), null);
            var algorithm = new QCAlgorithm();

            manager.SetAlgorithm(algorithm);

            var provider = typeof(QCAlgorithm)
                .GetField("_walkForwardOptimizationProvider", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(algorithm);
            Assert.IsInstanceOf<TestWalkForwardOptimizationProvider>(provider);
        }
    }

    public sealed class TestWalkForwardOptimizationProvider : IWalkForwardOptimizationProvider
    {
        public WalkForwardOptimizationResult Optimize(WalkForwardOptimizationRequest request)
        {
            return WalkForwardOptimizationResult.Empty;
        }
    }
}
