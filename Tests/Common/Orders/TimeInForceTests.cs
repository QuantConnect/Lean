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
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders
{
    [TestFixture]
    public class TimeInForceTests
    {
        [Test]
        public void SameTimeInForceTypesAreEqual()
        {
            var tif1 = new TimeInForce(TimeInForceType.GoodTilCanceled);
            var tif2 = new TimeInForce(TimeInForceType.GoodTilCanceled);

            Assert.IsTrue(tif1 == tif2);
            Assert.IsFalse(tif1 != tif2);

            Assert.IsTrue(tif1 == TimeInForce.GoodTilCanceled);
            Assert.IsFalse(tif1 != TimeInForce.GoodTilCanceled);

            Assert.AreEqual(tif1, tif2);
            Assert.AreEqual(tif1.Type, tif2.Type);

            Assert.AreEqual(tif1, TimeInForceType.GoodTilCanceled);
        }
    }
}
