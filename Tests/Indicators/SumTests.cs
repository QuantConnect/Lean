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

namespace QuantConnect.Tests.Indicators {

    [TestFixture]
    public class SumTests {

        [Test]
        public void ComputesCorrectly() {
            var sum = new Sum(2);
            var time = DateTime.UtcNow;

            sum.Update(time.AddDays(1), 1m);
            sum.Update(time.AddDays(1), 2m);
            sum.Update(time.AddDays(1), 3m);

            Assert.AreEqual(sum.Current.Value, 2m + 3m);
        }

        [Test]
        public void ResetsCorrectly() {
            var sum = new Sum(2);
            var time = DateTime.UtcNow;

            sum.Update(time.AddDays(1), 1m);
            sum.Update(time.AddDays(1), 2m);
            sum.Update(time.AddDays(1), 3m);

            Assert.IsTrue(sum.IsReady);

            sum.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(sum);
            Assert.AreEqual(sum.Current.Value, 0m);
            sum.Update(time.AddDays(1), 1);
            Assert.AreEqual(sum.Current.Value, 1m);
        }
    }
}
