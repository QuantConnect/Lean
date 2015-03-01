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
    public class IndicatorExtensionsTests
    {
        [Test]
        public void PipesDataUsingOfFromFirstToSecond()
        {
            var first = new SimpleMovingAverage(2);
            var second = new Delay(1);
            
            // this is a configuration step, but returns the reference to the second for method chaining
            second.Of(first);

            var data1 = new IndicatorDataPoint(DateTime.UtcNow, 1m);
            var data2 = new IndicatorDataPoint(DateTime.UtcNow, 2m);
            var data3 = new IndicatorDataPoint(DateTime.UtcNow, 3m);
            var data4 = new IndicatorDataPoint(DateTime.UtcNow, 4m);

            // sma has one item
            first.Update(data1);
            Assert.IsFalse(first.IsReady);
            Assert.AreEqual(0m, second.Current.Value);

            // sma is ready, delay will repeat this value
            first.Update(data2);
            Assert.IsTrue(first.IsReady);
            Assert.IsFalse(second.IsReady);
            Assert.AreEqual(1.5m, second.Current.Value);

            // delay is ready, and repeats its first input
            first.Update(data3);
            Assert.IsTrue(second.IsReady);
            Assert.AreEqual(1.5m, second.Current.Value);

            // now getting the delayed data
            first.Update(data4);
            Assert.AreEqual(2.5m, second.Current.Value);
        }

        [Test]
        public void NewDataPushesToDerivedIndicators()
        {
            var identity = new Identity("identity");
            var sma = new SimpleMovingAverage(3);

            identity.Updated += (sender, consolidated) =>
            {
                sma.Update(consolidated);
            };

            identity.Update(DateTime.UtcNow, 1m);
            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(sma.IsReady);
            Assert.AreEqual(2m, sma);
        }

        [Test]
        public void MultiChain()
        {
            var identity = new Identity("identity");
            var sma = new SimpleMovingAverage(2);
            var delay = new Delay(2);

            // create the SMA of the delay of the identity
            sma.Of(delay.Of(identity));

            identity.Update(DateTime.UtcNow, 1m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 2m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsFalse(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 3m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsFalse(sma.IsReady);

            identity.Update(DateTime.UtcNow, 4m);
            Assert.IsTrue(identity.IsReady);
            Assert.IsTrue(delay.IsReady);
            Assert.IsTrue(sma.IsReady);

            Assert.AreEqual(1.5m, sma);
        }

        [Test]
        public void PlusAddsLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Plus(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(2m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(2m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(2m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(7m, composite.Current.Value);
        }

        [Test]
        public void MinusSubtractsLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Minus(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(0m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(0m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(-1m, composite.Current.Value);
        }

        [Test]
        public void OverDivdesLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Over(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(3m / 4m, composite.Current.Value);
        }

        [Test]
        public void TimesMultipliesLeftAndRightAfterBothUpdated()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = left.Times(right);

            left.Update(DateTime.Today, 1m);
            right.Update(DateTime.Today, 1m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 2m);
            Assert.AreEqual(1m, composite.Current.Value);

            left.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, composite.Current.Value);

            right.Update(DateTime.Today, 4m);
            Assert.AreEqual(12m, composite.Current.Value);
        }
    }
}
