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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class MomentumTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            var mom =  new Momentum(5);
            foreach (var data in TestHelper.GetDataStream(5))
            {
                mom.Update(data);
                Assert.AreEqual(data.Value, mom.Current.Value);
            }
        }

        [Test]
        public void ResetsProperly()
        {
            var mom = new Momentum(5);
            foreach (var data in TestHelper.GetDataStream(6))
            {
                mom.Update(data);
            }
            Assert.IsTrue(mom.IsReady);

            mom.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(mom);
        }

        [Test]
        public void WarmsUpProperly()
        {
            var mom = new Momentum(5);
            var period = ((IIndicatorWarmUpPeriodProvider)mom).WarmUpPeriod;
            var dataStream = TestHelper.GetDataStream(period).ToArray();

            for (var i = 0; i < period; i++)
            {
                mom.Update(dataStream[i]);
                Assert.AreEqual(i == period - 1, mom.IsReady);
            }
            Assert.IsTrue(mom.IsReady);
            Assert.IsTrue(mom.Samples > mom.Period);
        }
    }
}