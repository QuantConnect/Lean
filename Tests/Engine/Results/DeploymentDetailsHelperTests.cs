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
using Moq;
using NUnit.Framework;
using QuantConnect.Util;
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class DeploymentDetailsHelperTests
    {
        [SetUp]
        public void SetUp()
        {
            Composer.Instance.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Composer.Instance.Reset();
        }

        [Test]
        public void AddsToTheComposerResultHandler()
        {
            var resultHandler = new TestResultHandler();
            Composer.Instance.AddPart<IResultHandler>(resultHandler);

            DeploymentDetailsHelper.Add("account", "123");
            DeploymentDetailsHelper.Add("environment", "paper");
            DeploymentDetailsHelper.Add("account", "456");

            Assert.AreEqual(2, resultHandler.DeploymentDetails.Count);
            Assert.AreEqual("456", resultHandler.DeploymentDetails["account"]);
            Assert.AreEqual("paper", resultHandler.DeploymentDetails["environment"]);
        }

        [Test]
        public void IgnoredWithoutResultHandler()
        {
            Assert.IsNull(Composer.Instance.GetPart<IResultHandler>());

            Assert.DoesNotThrow(() => DeploymentDetailsHelper.Add("account", "123"));
            Assert.DoesNotThrow(() => DeploymentDetailsHelper.Add("environment", "paper"));
        }

        [Test]
        public void DoesNotThrowOnResultHandlerFailure()
        {
            var resultHandler = new Mock<IResultHandler>();
            resultHandler.Setup(x => x.AddDeploymentDetail(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Some failure"));
            Composer.Instance.AddPart(resultHandler.Object);

            Assert.DoesNotThrow(() => DeploymentDetailsHelper.Add("account", "123"));
        }
    }
}
