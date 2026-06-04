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

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class MarketTests
    {
        [Test]
        public void MapsAllMarketsInMarketClass()
        {
            var markets = typeof(Market).GetFields();
            foreach (var field in markets)
            {
                var market = (string)field.GetValue(null);
                var code = Market.Encode(market);
                Assert.IsTrue(code.HasValue);

                var decoded = Market.Decode(code.Value);
                Assert.AreEqual(market, decoded);
            }
        }

        [Test]
        public void MapsChinaMarkets()
        {
            Assert.AreEqual(Market.SH, Market.Decode(Market.Encode(Market.SH).Value));
            Assert.AreEqual(Market.SZ, Market.Decode(Market.Encode(Market.SZ).Value));
            Assert.AreEqual(Market.CFFEX, Market.Decode(Market.Encode(Market.CFFEX).Value));
            Assert.AreEqual(Market.SHF, Market.Decode(Market.Encode(Market.SHF).Value));
            Assert.AreEqual(Market.DCE, Market.Decode(Market.Encode(Market.DCE).Value));
            Assert.AreEqual(Market.CZC, Market.Decode(Market.Encode(Market.CZC).Value));
            Assert.AreEqual(Market.INE, Market.Decode(Market.Encode(Market.INE).Value));
        }

        [TestCase("600000.SH", Market.SH)]
        [TestCase("000001.SZ", Market.SZ)]
        [TestCase("IF2506.CFE", Market.CFFEX)]
        [TestCase("RB2501.SHF", Market.SHF)]
        [TestCase("M2501.DCE", Market.DCE)]
        [TestCase("TA501.CZC", Market.CZC)]
        [TestCase("SC2501.INE", Market.INE)]
        public void MapsWindSuffixToMarket(string windTicker, string expectedMarket)
        {
            Assert.IsTrue(Market.TryGetMarketFromWindTicker(windTicker, out var market));
            Assert.AreEqual(expectedMarket, market);
        }
    }
}
