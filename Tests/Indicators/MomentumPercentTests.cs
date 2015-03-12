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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class MomentumPercentTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            var delay = new Delay(3);
            var sma = new SimpleMovingAverage(3);
            var momp = new MomentumPercent(3);
            foreach (var data in TestHelper.GetDataStream(4))
            {
                delay.Update(data);
                sma.Update(data);
                momp.Update(data);

                if (sma == 0m)
                {
                    Assert.AreEqual(0m, momp.Current.Value);
                }
                else
                {
                    decimal abs = data - delay;
                    decimal perc = abs / sma;
                    Assert.AreEqual(perc, momp.Current.Value);
                }
            }
        }

        [Test]
        public void ResetsProperly()
        {
            var momp = new MomentumPercent(3);
            foreach (var data in TestHelper.GetDataStream(4))
            {
                momp.Update(data);
            }
            Assert.IsTrue(momp.IsReady);
            Assert.IsTrue(momp.Average.IsReady);

            momp.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(momp);
            TestHelper.AssertIndicatorIsInDefaultState(momp.Average);
        }
    }
}