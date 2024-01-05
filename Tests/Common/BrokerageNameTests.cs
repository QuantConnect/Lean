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
using QuantConnect.Brokerages;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class BrokerageNameTests
    {
        [Test]
        public void BrokerageNameEnumsAreNumberedCorrectly()
        {
            Assert.AreEqual((int) BrokerageName.Default, 0);
            Assert.AreEqual((int) BrokerageName.QuantConnectBrokerage, 0);
            Assert.AreEqual((int) BrokerageName.InteractiveBrokersBrokerage, 1);
            Assert.AreEqual((int) BrokerageName.TradierBrokerage, 2);
            Assert.AreEqual((int) BrokerageName.OandaBrokerage, 3);
            Assert.AreEqual((int) BrokerageName.FxcmBrokerage, 4);
            Assert.AreEqual((int) BrokerageName.Bitfinex, 5);
            Assert.AreEqual((int) BrokerageName.Coinbase, 32);
        }
    }
}
