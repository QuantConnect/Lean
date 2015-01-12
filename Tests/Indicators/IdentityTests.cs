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
    /// <summary>
    /// Test class for QuantConnect.Indicators.Identity
    /// </summary>
    [TestFixture]
    public class IdentityTests
    {
        [Test]
        public void TestIdentityInvariants()
        {
            // the invariants of the identity indicator is to be ready after
            // a single sample has been added, and always produce the same value
            // as the last ingested value

            var identity = new Identity("test");
            Assert.IsFalse(identity.IsReady);

            const decimal value = 1m;
            identity.Update(new IndicatorDataPoint(DateTime.UtcNow, value));
            Assert.IsTrue(identity.IsReady);
            Assert.AreEqual(value, identity.Current.Value);
        }

        [Test]
        public void ResetsProperly()
        {
            var identity = new Identity("test");
            Assert.IsFalse(identity.IsReady);
            Assert.AreEqual(0m, identity.Current.Value);

            foreach (var data in TestHelper.GetDataStream(2))
            {
                identity.Update(data);
            }
            Assert.IsTrue(identity.IsReady);
            Assert.AreEqual(2, identity.Samples);

            identity.Reset();

            Assert.IsFalse(identity.IsReady);
            Assert.AreEqual(0, identity.Samples);
        }
    }
}
