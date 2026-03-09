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

using NUnit.Framework;
using QuantConnect.Research;
using QuantConnect.Configuration;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Tests.Research
{
    [TestFixture]
    public class QuantBookTests
    {
        [Test]
        public void AlgorithmModeIsResearch()
        {
            var qb = new QuantBook();
            Assert.AreEqual(AlgorithmMode.Research, qb.AlgorithmMode);
        }

        [TestCase(DeploymentTarget.CloudPlatform)]
        [TestCase(DeploymentTarget.LocalPlatform)]
        [TestCase(null)]
        public void SetsDeploymentTarget(DeploymentTarget? deploymentTarget)
        {
            Config.Reset();
            if (deploymentTarget.HasValue)
            {
                Config.Set("deployment-target", JToken.FromObject(deploymentTarget));
                Config.Write();
            }
            else
            {
                // The default value for deploymentTarget = DeploymentTarget.LocalPlatform
                deploymentTarget = DeploymentTarget.LocalPlatform;
            }

            var qb = new QuantBook();
            Assert.AreEqual(deploymentTarget, qb.DeploymentTarget);
        }
    }
}

