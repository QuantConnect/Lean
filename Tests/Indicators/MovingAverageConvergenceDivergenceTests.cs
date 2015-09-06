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
    public class MovingAverageConvergenceDivergenceTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            var fast = new SimpleMovingAverage(3);
            var slow = new SimpleMovingAverage(5);
            var signal = new SimpleMovingAverage(3);
            var macd = new MovingAverageConvergenceDivergence("macd", 3, 5, 3, MovingAverageType.Simple);

            foreach (var data in TestHelper.GetDataStream(7))
            {
                fast.Update(data);
                slow.Update(data);
                macd.Update(data);
                Assert.AreEqual(fast - slow, macd);
                if (fast.IsReady && slow.IsReady)
                {
                    signal.Update(new IndicatorDataPoint(data.Time, macd));
                    Assert.AreEqual(signal.Current.Value, macd.Current.Value);
                }
            }
        }

        [Test]
        public void ResetsProperly()
        {
            var macd = new MovingAverageConvergenceDivergence("macd", 3, 5, 3);
            foreach (var data in TestHelper.GetDataStream(30))
            {
                macd.Update(data);
            }
            Assert.IsTrue(macd.IsReady);

            macd.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(macd);
            TestHelper.AssertIndicatorIsInDefaultState(macd.Fast);
            TestHelper.AssertIndicatorIsInDefaultState(macd.Signal);
            TestHelper.AssertIndicatorIsInDefaultState(macd.Signal);
        }
    }
}
