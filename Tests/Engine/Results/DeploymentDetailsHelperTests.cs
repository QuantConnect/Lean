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
using QuantConnect.Util;
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class DeploymentDetailsHelperTests
    {
        [Test]
        public void AddsToTheResultHandlerInTheComposer()
        {
            // we explicitly use the result handler the composer resolves, adding one if there is none,
            // instead of resetting the composer which would drop the parts other tests rely on
            var resultHandler = Composer.Instance.GetPart<IResultHandler>();
            if (resultHandler == null)
            {
                resultHandler = new TestResultHandler();
                Composer.Instance.AddPart(resultHandler);
            }

            DeploymentDetailsHelper.Add("account", "123");
            DeploymentDetailsHelper.Add("environment", "paper");
            // updates in place
            DeploymentDetailsHelper.Add("account", "456");

            Assert.AreEqual("456", resultHandler.DeploymentDetails["account"]);
            Assert.AreEqual("paper", resultHandler.DeploymentDetails["environment"]);
        }
    }
}
