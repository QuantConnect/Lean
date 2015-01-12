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
    public class CompositeIndicatorTests
    {
        [Test]
        public void CompositeIsReadyWhenBothAre()
        {
            var composite = new CompositeIndicator<IndicatorDataPoint>(new Delay(1), new Delay(2), (left, right) => left + right);

            composite.Update(DateTime.Today.AddSeconds(0), 1m);
            Assert.IsFalse(composite.IsReady);
            Assert.IsFalse(composite.Left.IsReady);
            Assert.IsFalse(composite.Right.IsReady);

            composite.Update(DateTime.Today.AddSeconds(1), 2m);
            Assert.IsFalse(composite.IsReady);
            Assert.IsTrue(composite.Left.IsReady);
            Assert.IsFalse(composite.Right.IsReady);

            composite.Update(DateTime.Today.AddSeconds(2), 3m);
            Assert.IsTrue(composite.IsReady);
            Assert.IsTrue(composite.Left.IsReady);
            Assert.IsTrue(composite.Right.IsReady);

            composite.Update(DateTime.Today.AddSeconds(3), 4m);
            Assert.IsTrue(composite.IsReady);
            Assert.IsTrue(composite.Left.IsReady);
            Assert.IsTrue(composite.Right.IsReady);
        }

        [Test]
        public void CallsDelegateCorrectly()
        {
            var left = new Identity("left");
            var right = new Identity("right");
            var composite = new CompositeIndicator<IndicatorDataPoint>(left, right, (l, r) =>
            {
                Assert.AreEqual(left, l);
                Assert.AreEqual(right, r);
                return l + r;
            });

            composite.Update(DateTime.Today, 1m);
            Assert.AreEqual(2m, composite.Current.Value);
        }
    }
}
