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
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class IdentityDataConsolidatorTests
    {
        [Test]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ThrowsOnDataOfWrongType()
        {
            var identity = new IdentityDataConsolidator<Tick>();
            identity.Update(new TradeBar());
        }

        [Test]
        public void ReturnsTheSameObjectReference()
        {
            var identity = new IdentityDataConsolidator<Tick>();

            var tick = new Tick();

            int count = 0;
            identity.DataConsolidated += (sender, data) =>
            {
                Assert.IsTrue(ReferenceEquals(tick, data));
                count++;
            };

            identity.Update(tick);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void IgnoresNonTickDataWithSameTimestamps()
        {
            var reference = new DateTime(2015, 09, 23);
            var identity = new IdentityDataConsolidator<TradeBar>();

            int count = 0;
            identity.DataConsolidated += (sender, data) =>
            {
                count++;
            };
            
            var tradeBar = new TradeBar{EndTime = reference};
            identity.Update(tradeBar);

            tradeBar = (TradeBar) tradeBar.Clone();
            identity.Update(tradeBar);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AcceptsTickDataWithSameTimestamps()
        {
            var reference = new DateTime(2015, 09, 23);
            var identity = new IdentityDataConsolidator<Tick>();

            int count = 0;
            identity.DataConsolidated += (sender, data) =>
            {
                count++;
            };

            var tradeBar = new Tick { EndTime = reference };
            identity.Update(tradeBar);

            tradeBar = (Tick)tradeBar.Clone();
            identity.Update(tradeBar);

            Assert.AreEqual(2, count);
        }
    }
}
