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
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class FisherTransformTests
    {
        [Test]
        public void ComparesAgainstExternalData()
        {
            var fisher = new FisherTransform("fisher", 10);
            TestHelper.TestIndicator(fisher, "spy_with_fisher.txt", "Fisher Transform 10",
                (ind, expected) => Assert.AreEqual(expected, (double)((FisherTransform)ind).Current.Value, 1e-3));
        }

        [Test]
        public void ResetsProperly()
        {
            var fisher = new FisherTransform(10);
            //fisher.Update(DateTime.Today, 1m);
            //fisher.Update(DateTime.Today.AddSeconds(1), 2m);
            Assert.IsFalse(fisher.IsReady);

            fisher.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(fisher);
            //TestHelper.AssertIndicatorIsInDefaultState(fisher.AverageGain);
            //TestHelper.AssertIndicatorIsInDefaultState(fisher.AverageLoss);
        }
    }
}
