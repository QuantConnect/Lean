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
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class AlgorithmLogTests
    {
        [TestCase(typeof(TestAlgorithmWithErrorLogOnInit))]
        [TestCase(typeof(TestAlgorithmWithDebugLogOnInit))]
        [TestCase(typeof(TestAlgorithmWithLogOnInit))]
        public void AlgorithmCompletesWhenCallingErroLogOnInit(Type algorithmType)
        {
            var parameters = new RegressionTests.AlgorithmStatisticsTestParameters(
                "QuantConnect.Tests.Engine.AlgorithmLogTests+" + algorithmType.Name,
                Activator.CreateInstance<BasicTemplateDailyAlgorithm>().ExpectedStatistics,
                Language.CSharp,
                AlgorithmStatus.Completed
            );

            AlgorithmRunner.RunLocalBacktest(
                parameters.Algorithm,
                parameters.Statistics,
                parameters.Language,
                parameters.ExpectedFinalStatus,
                algorithmLocation: "QuantConnect.Tests.dll"
            );
        }

        public class TestAlgorithmWithErrorLogOnInit : BasicTemplateDailyAlgorithm
        {
            public override void Initialize()
            {
                base.Initialize();
                Error("Error in Initialize");
            }
        }

        public class TestAlgorithmWithDebugLogOnInit : BasicTemplateDailyAlgorithm
        {
            public override void Initialize()
            {
                base.Initialize();
                Debug("Error in Initialize");
            }
        }

        public class TestAlgorithmWithLogOnInit : BasicTemplateDailyAlgorithm
        {
            public override void Initialize()
            {
                base.Initialize();
                Log("Error in Initialize");
            }
        }
    }
}
