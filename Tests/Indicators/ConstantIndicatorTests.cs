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
    public class ConstantIndicatorTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            var cons = new ConstantIndicator<IndicatorDataPoint>("c", 1m);
            Assert.AreEqual(1m, cons.Current.Value);
            Assert.IsTrue(cons.IsReady);

            cons.Update(DateTime.Today, 3m);
            Assert.AreEqual(1m, cons.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            // constant reset should reset samples but the value should still be the same
            var cons = new ConstantIndicator<IndicatorDataPoint>("c", 1m);
            cons.Update(DateTime.Today, 3m);
            cons.Update(DateTime.Today.AddDays(1), 10m);

            cons.Reset();
            Assert.AreEqual(1m, cons.Current.Value);
            Assert.AreEqual(DateTime.MinValue, cons.Current.Time);
            Assert.AreEqual(0, cons.Samples);
        }
    }
}
