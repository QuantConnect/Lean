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
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class HoldingTests
    {
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Forex)]
        public void PriceRounding(SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            Security security = null;
            if (securityType == SecurityType.Equity)
            {
                security = algo.AddEquity("SPY");
            }
            else if (securityType == SecurityType.Crypto)
            {
                security = algo.AddCrypto("BNTBTC");
            }
            else if (securityType == SecurityType.Forex)
            {
                security = algo.AddForex("EURUSD");
            }
            else if (securityType == SecurityType.Future)
            {
                security = algo.AddFuture("CL");
            }
            security.SetMarketPrice(
                new Tick(new DateTime(2022, 01, 04), security.Symbol, 10.0001m, 10.0001m)
            );
            security.Holdings.SetHoldings(10.0000000000m, 10);

            var holding = new Holding(security);

            Assert.AreEqual(10, holding.Quantity);

            if (securityType == SecurityType.Equity || securityType == SecurityType.Future)
            {
                Assert.AreEqual(10.00m, holding.MarketPrice);
                Assert.AreEqual(10.00m, holding.AveragePrice);
            }
            else if (securityType == SecurityType.Forex || securityType == SecurityType.Crypto)
            {
                Assert.AreEqual(10.0001m, holding.MarketPrice);
                Assert.AreEqual(10.0000m, holding.AveragePrice);
            }
        }
    }
}
