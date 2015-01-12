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
    public class SequentialIndicatorTests
    {
        [Test]
        public void PipesDataFromFirstToSecond()
        {
            var first = new SimpleMovingAverage(2);
            var second = new Delay(1);
            var sequential = new SequentialIndicator<IndicatorDataPoint>(first, second);

            TestPipesData(sequential);
        }
        [Test]
        public void PipesDataUsingOfFromFirstToSecond()
        {
            var first = new SimpleMovingAverage(2);
            var second = new Delay(1);
            var sequential = second.Of(first);

            TestPipesData(sequential);
        }

        private static void TestPipesData(SequentialIndicator<IndicatorDataPoint> sequential)
        {
            var data1 = new IndicatorDataPoint(DateTime.UtcNow, 1m);
            var data2 = new IndicatorDataPoint(DateTime.UtcNow, 2m);
            var data3 = new IndicatorDataPoint(DateTime.UtcNow, 3m);
            var data4 = new IndicatorDataPoint(DateTime.UtcNow, 4m);
            var data5 = new IndicatorDataPoint(DateTime.UtcNow, 5m);

            // sma has one item
            sequential.Update(data1);
            Assert.IsFalse(sequential.IsReady);
            Assert.AreEqual(0m, sequential.Current.Value);

            // sma has two items
            sequential.Update(data2);
            Assert.IsFalse(sequential.IsReady);
            Assert.AreEqual(0m, sequential.Current.Value);

            // sma is ready, delay will repeat this value
            sequential.Update(data3);
            Assert.IsFalse(sequential.IsReady);
            Assert.AreEqual(2.5m, sequential.Current.Value);

            // delay is ready, and repeats its first input
            sequential.Update(data4);
            Assert.IsTrue(sequential.IsReady);
            Assert.AreEqual(2.5m, sequential.Current.Value);

            // now getting the delayed data
            sequential.Update(data5);
            Assert.IsTrue(sequential.IsReady);
            Assert.AreEqual(3.5m, sequential.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            var first = new Delay(1);
            var second = new Delay(1);
            var sequential = new SequentialIndicator<IndicatorDataPoint>(first, second);

            foreach (var data in TestHelper.GetDataStream(3))
            {
                sequential.Update(data);
            }
            Assert.IsTrue(first.IsReady);
            Assert.IsTrue(second.IsReady);
            Assert.IsTrue(sequential.IsReady);

            sequential.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(first);
            TestHelper.AssertIndicatorIsInDefaultState(second);
            TestHelper.AssertIndicatorIsInDefaultState(sequential);
        }
    }
}
