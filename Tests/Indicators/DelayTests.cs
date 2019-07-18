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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class DelayTests
    {
        [Test, ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "size of at least 1")]
        public void DelayZeroThrowsArgumentException()
        {
            new Delay(0);
        }

        [Test]
        public void DelayOneRepeatsFirstInputValue()
        {
            var delay = new Delay(1);

            var data = new IndicatorDataPoint(DateTime.UtcNow, 1m);
            delay.Update(data);
            Assert.AreEqual(1m, delay.Current.Value);

            data = new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(1), 2m);
            delay.Update(data);
            Assert.AreEqual(1m, delay.Current.Value);

            data = new IndicatorDataPoint(DateTime.UtcNow.AddSeconds(1), 2m);
            delay.Update(data);
            Assert.AreEqual(2m, delay.Current.Value);
        }

        [Test]
        public void DelayTakesPeriodPlus2UpdatesToEmitNonInitialPoint()
        {
            const int start = 1;
            const int count = 10;
            for (var i = start; i < count+start; i++)
            {
                TestDelayTakesPeriodPlus2UpdatesToEmitNonInitialPoint(i);
            }
        }

        private void TestDelayTakesPeriodPlus2UpdatesToEmitNonInitialPoint(int period)
        {
            var delay = new Delay(period);
            for (var i = 0; i < period + 2; i++)
            {
                Assert.AreEqual(0m, delay.Current.Value);
                delay.Update(new IndicatorDataPoint(DateTime.Today.AddSeconds(i), i));
            }
            Assert.AreEqual(1m, delay.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            var delay = new Delay(2);

            foreach (var data in TestHelper.GetDataStream(3))
            {
                delay.Update(data);
            }
            Assert.IsTrue(delay.IsReady);
            Assert.AreEqual(3, delay.Samples);

            delay.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(delay);
        }

        [Test]
        public void WarmsUpProperly()
        {
            var delay = new Delay(20);
            var count = ((IIndicatorWarmUpPeriodProvider) delay).WarmUpPeriod;
            var dataArray = TestHelper.GetDataStream(count).ToArray();

            for (var i = 0; i < count; i++)
            {
                delay.Update(dataArray[i]);
                Assert.AreEqual(i == count - 1, delay.IsReady);
            }
        }
    }
}