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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class DataPermissionManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            Config.Set("data-permission-manager", "TestDataPermissionManager");
            TestInvalidConfigurationAlgorithm.Count = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Config.Reset();
        }

        [Test]
        public void InvalidConfigurationAddSecurity()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(BasicTemplateDailyAlgorithm),
                new Dictionary<string, string>(),
                Language.CSharp,
                // will throw on initialization
                AlgorithmStatus.Running);

            var result = AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus);

            // algorithm was never set
            Assert.IsEmpty(result.AlgorithmManager.AlgorithmId);
        }

        [Test]
        public void InvalidConfigurationHistoryRequest()
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(nameof(TestInvalidConfigurationAlgorithm),
                new Dictionary<string, string>(),
                Language.CSharp,
                // will throw on initialization
                AlgorithmStatus.Running);

            var result = AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.AlphaStatistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                setupHandler: "TestInvalidConfigurationSetupHandler");

            // algorithm was never set
            Assert.IsEmpty(result.AlgorithmManager.AlgorithmId);
            // let's assert initialize was called by the history call failed
            Assert.AreEqual(1, TestInvalidConfigurationAlgorithm.Count);
        }

        public class TestDataPermissionManager : DataPermissionManager
        {
            public override void AssertConfiguration(SubscriptionDataConfig subscriptionDataConfig)
            {
                throw new InvalidOperationException("Invalid configuration");
            }
        }

        public class TestInvalidConfigurationAlgorithm : BasicTemplateDailyAlgorithm
        {
            public static int Count;
            public override void Initialize()
            {
                Count++;
                History("SPY", 1, Resolution.Tick).ToList();
                Count++;
            }
        }
        
        public class TestInvalidConfigurationSetupHandler : AlgorithmRunner.RegressionSetupHandlerWrapper
        {
            public override IAlgorithm CreateAlgorithmInstance(AlgorithmNodePacket algorithmNodePacket, string assemblyPath)
            {
                Algorithm = new TestInvalidConfigurationAlgorithm();
                return Algorithm;
            }
        }
    }
}
